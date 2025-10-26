/**
 * Monster Entity
 * Represents a hostile creature in the game
 */

import { Entity, IPosition, IEntityData } from './Entity';

export interface IMonsterStats {
  hp: number;
  maxHp: number;
  damage: number;
  defense: number;
  ap?: number;
  mp?: number;
  resistance?: {
    fire?: number;
    water?: number;
    earth?: number;
    air?: number;
    neutral?: number;
  };
}

export interface IMonsterDrop {
  itemId: string;
  itemName: string;
  chance: number; // 0-100
  minQuantity: number;
  maxQuantity: number;
}

export interface IMonsterData extends IEntityData {
  level: number;
  stats: IMonsterStats;
  aggroRange: number;
  respawnTime: number; // in seconds
  drops: IMonsterDrop[];
  experience: number;
  sprite?: string;
  family?: string;
  isBoss?: boolean;
  abilities?: string[];
}

/**
 * Monster entity class
 */
export class Monster extends Entity {
  public level: number;
  public stats: IMonsterStats;
  public aggroRange: number;
  public respawnTime: number;
  public drops: IMonsterDrop[];
  public experience: number;
  public sprite?: string;
  public family?: string;
  public isBoss: boolean;
  public abilities: string[];
  public isDead: boolean;
  public isInCombat: boolean;
  public currentTarget?: string; // Player ID
  private respawnTimer?: NodeJS.Timeout;
  private lastDeathTime?: number;

  constructor(data: IMonsterData) {
    super(data);
    this.level = data.level;
    this.stats = { ...data.stats };
    this.aggroRange = data.aggroRange;
    this.respawnTime = data.respawnTime;
    this.drops = data.drops || [];
    this.experience = data.experience;
    this.sprite = data.sprite;
    this.family = data.family;
    this.isBoss = data.isBoss || false;
    this.abilities = data.abilities || [];
    this.isDead = false;
    this.isInCombat = false;
  }

  /**
   * Apply damage to the monster
   */
  public takeDamage(damage: number, damageType?: string): number {
    if (this.isDead) {
      return 0;
    }

    // Calculate resistance if damage type is specified
    let finalDamage = damage;
    if (damageType && this.stats.resistance) {
      const resistance = this.stats.resistance[damageType as keyof typeof this.stats.resistance] || 0;
      finalDamage = Math.max(1, damage - resistance);
    }

    // Apply defense
    finalDamage = Math.max(1, finalDamage - this.stats.defense);

    this.stats.hp = Math.max(0, this.stats.hp - finalDamage);

    if (this.stats.hp === 0) {
      this.die();
    }

    return finalDamage;
  }

  /**
   * Attack a target
   */
  public attack(targetId: string): number {
    if (this.isDead || !this.isInCombat) {
      return 0;
    }

    // Basic attack damage calculation
    const damage = this.stats.damage;
    this.currentTarget = targetId;

    return damage;
  }

  /**
   * Check if monster should aggro on a player
   */
  public shouldAggro(playerPosition: IPosition): boolean {
    if (this.isDead || this.isInCombat) {
      return false;
    }

    // Check if same map
    if (playerPosition.mapId !== this.position.mapId) {
      return false;
    }

    // Calculate distance (simplified - would need proper pathfinding)
    const distance = this.calculateDistance(playerPosition.cellId, this.position.cellId);

    return distance <= this.aggroRange;
  }

  /**
   * Calculate distance between cells (simplified)
   */
  private calculateDistance(cellId1: number, cellId2: number): number {
    // Simplified distance calculation
    // In a real implementation, this would use the actual map grid
    const cell1X = cellId1 % 100;
    const cell1Y = Math.floor(cellId1 / 100);
    const cell2X = cellId2 % 100;
    const cell2Y = Math.floor(cellId2 / 100);

    return Math.sqrt(Math.pow(cell2X - cell1X, 2) + Math.pow(cell2Y - cell1Y, 2));
  }

  /**
   * Handle monster death
   */
  private die(): void {
    this.isDead = true;
    this.isInCombat = false;
    this.lastDeathTime = Date.now();
    this.currentTarget = undefined;

    // Schedule respawn
    this.scheduleRespawn();
  }

  /**
   * Schedule monster respawn
   */
  private scheduleRespawn(): void {
    if (this.respawnTimer) {
      clearTimeout(this.respawnTimer);
    }

    this.respawnTimer = setTimeout(() => {
      this.respawn();
    }, this.respawnTime * 1000);
  }

  /**
   * Respawn the monster
   */
  public respawn(): void {
    this.isDead = false;
    this.isInCombat = false;
    this.stats.hp = this.stats.maxHp;
    this.currentTarget = undefined;
    this.lastDeathTime = undefined;

    if (this.respawnTimer) {
      clearTimeout(this.respawnTimer);
      this.respawnTimer = undefined;
    }
  }

  /**
   * Generate loot drops
   */
  public drop(): IMonsterDrop[] {
    const droppedItems: IMonsterDrop[] = [];

    for (const drop of this.drops) {
      const roll = Math.random() * 100;

      if (roll <= drop.chance) {
        const quantity = Math.floor(
          Math.random() * (drop.maxQuantity - drop.minQuantity + 1) + drop.minQuantity
        );

        droppedItems.push({
          ...drop,
          minQuantity: quantity,
          maxQuantity: quantity,
        });
      }
    }

    return droppedItems;
  }

  /**
   * Enter combat state
   */
  public enterCombat(targetId?: string): void {
    this.isInCombat = true;
    if (targetId) {
      this.currentTarget = targetId;
    }
  }

  /**
   * Leave combat state
   */
  public leaveCombat(): void {
    this.isInCombat = false;
    this.currentTarget = undefined;
  }

  /**
   * Heal the monster (for special abilities or events)
   */
  public heal(amount: number): void {
    if (this.isDead) {
      return;
    }

    this.stats.hp = Math.min(this.stats.maxHp, this.stats.hp + amount);
  }

  /**
   * Check if monster has a specific ability
   */
  public hasAbility(abilityName: string): boolean {
    return this.abilities.includes(abilityName);
  }

  /**
   * Use action points
   */
  public useAp(amount: number): boolean {
    if (!this.stats.ap || this.stats.ap < amount) {
      return false;
    }

    this.stats.ap -= amount;
    return true;
  }

  /**
   * Use movement points
   */
  public useMp(amount: number): boolean {
    if (!this.stats.mp || this.stats.mp < amount) {
      return false;
    }

    this.stats.mp -= amount;
    return true;
  }

  /**
   * Get time since last death
   */
  public getTimeSinceDeath(): number | null {
    if (!this.lastDeathTime) {
      return null;
    }

    return Date.now() - this.lastDeathTime;
  }

  /**
   * Get remaining respawn time
   */
  public getRemainingRespawnTime(): number {
    if (!this.isDead || !this.lastDeathTime) {
      return 0;
    }

    const elapsed = (Date.now() - this.lastDeathTime) / 1000;
    const remaining = Math.max(0, this.respawnTime - elapsed);

    return Math.ceil(remaining);
  }

  /**
   * Cleanup timers (call when removing monster)
   */
  public cleanup(): void {
    if (this.respawnTimer) {
      clearTimeout(this.respawnTimer);
      this.respawnTimer = undefined;
    }
  }

  /**
   * Serialize monster data
   */
  public toJSON(): Record<string, any> {
    return {
      id: this.id,
      name: this.name,
      level: this.level,
      position: this.position,
      stats: this.stats,
      aggroRange: this.aggroRange,
      respawnTime: this.respawnTime,
      drops: this.drops,
      experience: this.experience,
      sprite: this.sprite,
      family: this.family,
      isBoss: this.isBoss,
      abilities: this.abilities,
      isDead: this.isDead,
      isInCombat: this.isInCombat,
      currentTarget: this.currentTarget,
      remainingRespawnTime: this.getRemainingRespawnTime(),
    };
  }
}
