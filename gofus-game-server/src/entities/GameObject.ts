/**
 * GameObject Entity
 * Represents an interactive object in the game world
 */

import { Entity, IPosition, IEntityData } from './Entity';

export enum GameObjectType {
  DOOR = 'door',
  CHEST = 'chest',
  LEVER = 'lever',
  TELEPORTER = 'teleporter',
  RESOURCE_NODE = 'resource_node',
  CRAFTING_STATION = 'crafting_station',
  DECORATIVE = 'decorative',
  QUEST_OBJECT = 'quest_object',
  TRAP = 'trap',
}

export interface IGameObjectRequirement {
  type: 'level' | 'item' | 'quest' | 'skill' | 'key';
  value: string | number;
  message?: string;
}

export interface IGameObjectReward {
  type: 'item' | 'experience' | 'kamas' | 'teleport';
  value: string | number;
  quantity?: number;
}

export interface IGameObjectData extends IEntityData {
  type: GameObjectType;
  interactive: boolean;
  usable: boolean;
  requirements?: IGameObjectRequirement[];
  rewards?: IGameObjectReward[];
  cooldown?: number; // in seconds
  maxUses?: number;
  sprite?: string;
  state?: string;
  questId?: string;
}

/**
 * GameObject entity class
 */
export class GameObject extends Entity {
  public type: GameObjectType;
  public interactive: boolean;
  public usable: boolean;
  public requirements: IGameObjectRequirement[];
  public rewards: IGameObjectReward[];
  public cooldown: number;
  public maxUses: number;
  public sprite?: string;
  public state: string;
  public questId?: string;
  private currentUses: number;
  private lastUsedBy: Map<string, number>; // playerId -> timestamp
  private isLocked: boolean;

  constructor(data: IGameObjectData) {
    super(data);
    this.type = data.type;
    this.interactive = data.interactive;
    this.usable = data.usable;
    this.requirements = data.requirements || [];
    this.rewards = data.rewards || [];
    this.cooldown = data.cooldown || 0;
    this.maxUses = data.maxUses || Infinity;
    this.sprite = data.sprite;
    this.state = data.state || 'default';
    this.questId = data.questId;
    this.currentUses = 0;
    this.lastUsedBy = new Map();
    this.isLocked = false;
  }

  /**
   * Use the game object
   */
  public use(playerId: string): IGameObjectReward[] | null {
    if (!this.canInteract(playerId)) {
      return null;
    }

    // Check if object has uses left
    if (this.currentUses >= this.maxUses) {
      return null;
    }

    // Check cooldown for this player
    if (!this.isOffCooldown(playerId)) {
      return null;
    }

    // Increment use count
    this.currentUses++;
    this.lastUsedBy.set(playerId, Date.now());

    // Execute object-specific behavior
    this.onUse(playerId);

    // Return rewards
    return this.rewards.length > 0 ? [...this.rewards] : null;
  }

  /**
   * Check if a player can interact with this object
   */
  public canInteract(playerId: string, playerData?: any): boolean {
    if (!this.interactive || !this.usable) {
      return false;
    }

    if (this.isLocked) {
      return false;
    }

    // Check requirements
    for (const requirement of this.requirements) {
      if (!this.meetsRequirement(requirement, playerData)) {
        return false;
      }
    }

    return true;
  }

  /**
   * Check if a requirement is met
   */
  private meetsRequirement(requirement: IGameObjectRequirement, playerData?: any): boolean {
    if (!playerData) {
      return true; // Can't verify without player data
    }

    switch (requirement.type) {
      case 'level':
        return playerData.level >= requirement.value;
      case 'item':
        return playerData.hasItem && playerData.hasItem(requirement.value);
      case 'quest':
        return playerData.hasCompletedQuest && playerData.hasCompletedQuest(requirement.value);
      case 'skill':
        return playerData.hasSkill && playerData.hasSkill(requirement.value);
      case 'key':
        return playerData.hasKey && playerData.hasKey(requirement.value);
      default:
        return true;
    }
  }

  /**
   * Check if object is off cooldown for a player
   */
  public isOffCooldown(playerId: string): boolean {
    if (this.cooldown === 0) {
      return true;
    }

    const lastUsed = this.lastUsedBy.get(playerId);
    if (!lastUsed) {
      return true;
    }

    const timeSinceUse = (Date.now() - lastUsed) / 1000;
    return timeSinceUse >= this.cooldown;
  }

  /**
   * Get remaining cooldown time for a player
   */
  public getRemainingCooldown(playerId: string): number {
    const lastUsed = this.lastUsedBy.get(playerId);
    if (!lastUsed) {
      return 0;
    }

    const timeSinceUse = (Date.now() - lastUsed) / 1000;
    const remaining = Math.max(0, this.cooldown - timeSinceUse);

    return Math.ceil(remaining);
  }

  /**
   * Handle object-specific behavior when used
   */
  private onUse(playerId: string): void {
    switch (this.type) {
      case GameObjectType.DOOR:
        this.toggleDoor();
        break;
      case GameObjectType.CHEST:
        this.openChest();
        break;
      case GameObjectType.LEVER:
        this.toggleLever();
        break;
      case GameObjectType.RESOURCE_NODE:
        this.harvestResource();
        break;
      default:
        // Generic interaction
        this.state = 'used';
    }
  }

  /**
   * Toggle door state
   */
  private toggleDoor(): void {
    this.state = this.state === 'open' ? 'closed' : 'open';
  }

  /**
   * Open chest
   */
  private openChest(): void {
    if (this.state !== 'opened') {
      this.state = 'opened';
    }
  }

  /**
   * Toggle lever
   */
  private toggleLever(): void {
    this.state = this.state === 'on' ? 'off' : 'on';
  }

  /**
   * Harvest resource
   */
  private harvestResource(): void {
    this.state = 'depleted';
    // Resource nodes typically respawn after cooldown
  }

  /**
   * Lock the object
   */
  public lock(): void {
    this.isLocked = true;
  }

  /**
   * Unlock the object
   */
  public unlock(): void {
    this.isLocked = false;
  }

  /**
   * Reset the object to initial state
   */
  public reset(): void {
    this.currentUses = 0;
    this.lastUsedBy.clear();
    this.state = 'default';
    this.isLocked = false;
  }

  /**
   * Reset for a specific player
   */
  public resetForPlayer(playerId: string): void {
    this.lastUsedBy.delete(playerId);
  }

  /**
   * Get current state
   */
  public getState(): string {
    return this.state;
  }

  /**
   * Set state
   */
  public setState(state: string): void {
    this.state = state;
  }

  /**
   * Check if object is a door
   */
  public isDoor(): boolean {
    return this.type === GameObjectType.DOOR;
  }

  /**
   * Check if object is a teleporter
   */
  public isTeleporter(): boolean {
    return this.type === GameObjectType.TELEPORTER;
  }

  /**
   * Check if object is a resource node
   */
  public isResourceNode(): boolean {
    return this.type === GameObjectType.RESOURCE_NODE;
  }

  /**
   * Check if object is quest-related
   */
  public isQuestObject(): boolean {
    return this.type === GameObjectType.QUEST_OBJECT && !!this.questId;
  }

  /**
   * Get remaining uses
   */
  public getRemainingUses(): number {
    if (this.maxUses === Infinity) {
      return Infinity;
    }
    return Math.max(0, this.maxUses - this.currentUses);
  }

  /**
   * Check if object can still be used
   */
  public hasUsesRemaining(): boolean {
    return this.currentUses < this.maxUses;
  }

  /**
   * Add a requirement
   */
  public addRequirement(requirement: IGameObjectRequirement): void {
    this.requirements.push(requirement);
  }

  /**
   * Remove a requirement
   */
  public removeRequirement(requirementType: string): boolean {
    const index = this.requirements.findIndex(r => r.type === requirementType);
    if (index !== -1) {
      this.requirements.splice(index, 1);
      return true;
    }
    return false;
  }

  /**
   * Add a reward
   */
  public addReward(reward: IGameObjectReward): void {
    this.rewards.push(reward);
  }

  /**
   * Clear all rewards
   */
  public clearRewards(): void {
    this.rewards = [];
  }

  /**
   * Serialize object data
   */
  public toJSON(): Record<string, any> {
    return {
      id: this.id,
      name: this.name,
      type: this.type,
      position: this.position,
      interactive: this.interactive,
      usable: this.usable,
      requirements: this.requirements,
      rewards: this.rewards,
      cooldown: this.cooldown,
      maxUses: this.maxUses,
      currentUses: this.currentUses,
      sprite: this.sprite,
      state: this.state,
      questId: this.questId,
      isLocked: this.isLocked,
      remainingUses: this.getRemainingUses(),
    };
  }
}
