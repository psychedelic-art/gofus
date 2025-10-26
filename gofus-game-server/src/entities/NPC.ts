/**
 * NPC Entity
 * Represents a non-player character in the game
 */

import { Entity, IPosition, IEntityData } from './Entity';

export enum NPCType {
  QUEST_GIVER = 'quest_giver',
  MERCHANT = 'merchant',
  BANKER = 'banker',
  TRAINER = 'trainer',
  DECORATOR = 'decorator',
  GENERIC = 'generic',
}

export interface IDialogue {
  id: string;
  text: string;
  responses?: IDialogueResponse[];
  conditions?: Record<string, any>;
}

export interface IDialogueResponse {
  id: string;
  text: string;
  nextDialogueId?: string;
  action?: string;
  actionParams?: Record<string, any>;
}

export interface INPCAction {
  type: string;
  id: string;
  params?: Record<string, any>;
  requirements?: Record<string, any>;
}

export interface INPCData extends IEntityData {
  type: NPCType;
  dialogues: IDialogue[];
  actions?: INPCAction[];
  questId?: string;
  shopId?: string;
  sprite?: string;
  level?: number;
}

/**
 * NPC entity class
 */
export class NPC extends Entity {
  public type: NPCType;
  public dialogues: IDialogue[];
  public actions: INPCAction[];
  public questId?: string;
  public shopId?: string;
  public sprite?: string;
  public level?: number;
  private currentDialogueIndex: Map<string, string>; // playerId -> dialogueId

  constructor(data: INPCData) {
    super(data);
    this.type = data.type;
    this.dialogues = data.dialogues || [];
    this.actions = data.actions || [];
    this.questId = data.questId;
    this.shopId = data.shopId;
    this.sprite = data.sprite;
    this.level = data.level;
    this.currentDialogueIndex = new Map();
  }

  /**
   * Interact with the NPC
   */
  public interact(playerId: string, action?: string): IDialogue | INPCAction | null {
    // If a specific action is requested
    if (action) {
      const npcAction = this.actions.find(a => a.type === action);
      if (npcAction && this.canPerformAction(npcAction, playerId)) {
        return npcAction;
      }
      return null;
    }

    // Otherwise, return the first/current dialogue
    return this.getDialogue(playerId);
  }

  /**
   * Get the current or initial dialogue for a player
   */
  public getDialogue(playerId: string, dialogueId?: string): IDialogue | null {
    if (this.dialogues.length === 0) {
      return null;
    }

    // If specific dialogue ID is provided
    if (dialogueId) {
      const dialogue = this.dialogues.find(d => d.id === dialogueId);
      if (dialogue) {
        this.currentDialogueIndex.set(playerId, dialogueId);
        return this.filterDialogueByConditions(dialogue, playerId);
      }
    }

    // Get current dialogue or first one
    const currentId = this.currentDialogueIndex.get(playerId);
    if (currentId) {
      const dialogue = this.dialogues.find(d => d.id === currentId);
      if (dialogue) {
        return this.filterDialogueByConditions(dialogue, playerId);
      }
    }

    // Return first dialogue
    const firstDialogue = this.dialogues[0];
    this.currentDialogueIndex.set(playerId, firstDialogue.id);
    return this.filterDialogueByConditions(firstDialogue, playerId);
  }

  /**
   * Filter dialogue based on conditions
   */
  private filterDialogueByConditions(dialogue: IDialogue, playerId: string): IDialogue {
    // Could implement condition checking here
    // For now, return the dialogue as-is
    return dialogue;
  }

  /**
   * Handle a dialogue response from a player
   */
  public handleResponse(playerId: string, responseId: string): IDialogue | null {
    const currentId = this.currentDialogueIndex.get(playerId);
    if (!currentId) {
      return null;
    }

    const currentDialogue = this.dialogues.find(d => d.id === currentId);
    if (!currentDialogue || !currentDialogue.responses) {
      return null;
    }

    const response = currentDialogue.responses.find(r => r.id === responseId);
    if (!response) {
      return null;
    }

    // Execute any action associated with the response
    if (response.action) {
      this.executeAction(response.action, response.actionParams, playerId);
    }

    // Move to next dialogue if specified
    if (response.nextDialogueId) {
      return this.getDialogue(playerId, response.nextDialogueId);
    }

    // Reset dialogue
    this.currentDialogueIndex.delete(playerId);
    return null;
  }

  /**
   * Execute an action
   */
  private executeAction(actionType: string, params: Record<string, any> | undefined, playerId: string): void {
    // This would trigger actions like:
    // - Starting a quest
    // - Opening a shop
    // - Giving items
    // - Teleporting
    // etc.
    // Implementation would be handled by the game manager
  }

  /**
   * Check if an action can be performed
   */
  private canPerformAction(action: INPCAction, playerId: string): boolean {
    if (!action.requirements) {
      return true;
    }

    // Check requirements (level, quest completion, items, etc.)
    // Implementation would check against player state
    return true;
  }

  /**
   * Add a new dialogue
   */
  public addDialogue(dialogue: IDialogue): void {
    this.dialogues.push(dialogue);
  }

  /**
   * Remove a dialogue by ID
   */
  public removeDialogue(dialogueId: string): boolean {
    const index = this.dialogues.findIndex(d => d.id === dialogueId);
    if (index !== -1) {
      this.dialogues.splice(index, 1);
      return true;
    }
    return false;
  }

  /**
   * Add a new action
   */
  public addAction(action: INPCAction): void {
    this.actions.push(action);
  }

  /**
   * Remove an action by type
   */
  public removeAction(actionType: string): boolean {
    const index = this.actions.findIndex(a => a.type === actionType);
    if (index !== -1) {
      this.actions.splice(index, 1);
      return true;
    }
    return false;
  }

  /**
   * Reset dialogue state for a player
   */
  public resetDialogue(playerId: string): void {
    this.currentDialogueIndex.delete(playerId);
  }

  /**
   * Check if NPC is a quest giver
   */
  public isQuestGiver(): boolean {
    return this.type === NPCType.QUEST_GIVER && !!this.questId;
  }

  /**
   * Check if NPC is a merchant
   */
  public isMerchant(): boolean {
    return this.type === NPCType.MERCHANT && !!this.shopId;
  }

  /**
   * Serialize NPC data
   */
  public toJSON(): Record<string, any> {
    return {
      id: this.id,
      name: this.name,
      type: this.type,
      position: this.position,
      dialogues: this.dialogues,
      actions: this.actions,
      questId: this.questId,
      shopId: this.shopId,
      sprite: this.sprite,
      level: this.level,
    };
  }
}
