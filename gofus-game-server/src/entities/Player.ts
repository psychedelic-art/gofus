/**
 * Player Entity
 * Represents a player character in the game
 */

import { Entity, IPosition, IEntityData } from './Entity';

export interface IPlayerStats {
  hp: number;
  maxHp: number;
  mp: number;
  maxMp: number;
  ap: number;
  maxAp: number;
  movement: number;
}

export interface IPlayerCharacteristics {
  vitality: number;
  wisdom: number;
  strength: number;
  intelligence: number;
  chance: number;
  agility: number;
}

export interface IPlayerData extends IEntityData {
  characterId: string;
  accountId: string;
  level: number;
  classId: number;
  stats: IPlayerStats;
  characteristics: IPlayerCharacteristics;
  kamas: number;
  experience: number;
  isInCombat?: boolean;
  isDead?: boolean;
}

/**
 * Player entity class
 */
export class Player extends Entity {
  public characterId: string;
  public accountId: string;
  public level: number;
  public classId: number;
  public stats: IPlayerStats;
  public characteristics: IPlayerCharacteristics;
  public kamas: number;
  public experience: number;
  public isInCombat: boolean;
  public isDead: boolean;

  constructor(data: IPlayerData) {
    super(data);
    this.characterId = data.characterId;
    this.accountId = data.accountId;
    this.level = data.level;
    this.classId = data.classId;
    this.stats = { ...data.stats };
    this.characteristics = { ...data.characteristics };
    this.kamas = data.kamas;
    this.experience = data.experience;
    this.isInCombat = data.isInCombat || false;
    this.isDead = data.isDead || false;
  }

  /**
   * Apply damage to the player
   */
  public takeDamage(damage: number): void {
    if (this.isDead) {
      return;
    }

    this.stats.hp = Math.max(0, this.stats.hp - damage);

    if (this.stats.hp === 0) {
      this.die();
    }
  }

  /**
   * Heal the player
   */
  public heal(amount: number): void {
    if (this.isDead) {
      return;
    }

    this.stats.hp = Math.min(this.stats.maxHp, this.stats.hp + amount);
  }

  /**
   * Add experience to the player
   */
  public addExperience(amount: number): boolean {
    this.experience += amount;
    return this.checkLevelUp();
  }

  /**
   * Check if player should level up
   */
  private checkLevelUp(): boolean {
    const experienceNeeded = this.getExperienceForLevel(this.level + 1);

    if (this.experience >= experienceNeeded) {
      this.levelUp();
      return true;
    }

    return false;
  }

  /**
   * Calculate experience needed for a specific level
   */
  private getExperienceForLevel(level: number): number {
    // Basic formula: level * 1000 (can be customized)
    return level * 1000;
  }

  /**
   * Level up the player
   */
  private levelUp(): void {
    this.level++;

    // Increase base stats on level up
    this.stats.maxHp += 10;
    this.stats.maxMp += 5;
    this.stats.hp = this.stats.maxHp;
    this.stats.mp = this.stats.maxMp;

    // Could emit level up event here
  }

  /**
   * Handle player death
   */
  private die(): void {
    this.isDead = true;
    this.isInCombat = false;
    // Could emit death event here
  }

  /**
   * Respawn the player
   */
  public respawn(respawnMapId: number, respawnCellId: number): void {
    this.isDead = false;
    this.stats.hp = this.stats.maxHp;
    this.stats.mp = this.stats.maxMp;
    this.updatePosition(respawnMapId, respawnCellId);
  }

  /**
   * Add or remove kamas
   */
  public modifyKamas(amount: number): void {
    this.kamas = Math.max(0, this.kamas + amount);
  }

  /**
   * Check if player can afford something
   */
  public canAfford(cost: number): boolean {
    return this.kamas >= cost;
  }

  /**
   * Enter combat state
   */
  public enterCombat(): void {
    this.isInCombat = true;
  }

  /**
   * Leave combat state
   */
  public leaveCombat(): void {
    this.isInCombat = false;
  }

  /**
   * Restore AP and MP (typically at start of turn)
   */
  public restorePoints(): void {
    this.stats.ap = this.stats.maxAp;
    this.stats.mp = this.stats.maxMp;
  }

  /**
   * Use action points
   */
  public useAp(amount: number): boolean {
    if (this.stats.ap >= amount) {
      this.stats.ap -= amount;
      return true;
    }
    return false;
  }

  /**
   * Use movement points
   */
  public useMp(amount: number): boolean {
    if (this.stats.mp >= amount) {
      this.stats.mp -= amount;
      return true;
    }
    return false;
  }

  /**
   * Serialize player data
   */
  public toJSON(): Record<string, any> {
    return {
      id: this.id,
      characterId: this.characterId,
      accountId: this.accountId,
      name: this.name,
      level: this.level,
      classId: this.classId,
      position: this.position,
      stats: this.stats,
      characteristics: this.characteristics,
      kamas: this.kamas,
      experience: this.experience,
      isInCombat: this.isInCombat,
      isDead: this.isDead,
    };
  }
}
