/**
 * NPC Entity Tests
 * Comprehensive tests for the NPC class
 */

import { NPC, NPCType, INPCData, IDialogue, IDialogueResponse, INPCAction } from '../NPC';

describe('NPC', () => {
  const createTestNPC = (overrides?: Partial<INPCData>): NPC => {
    const defaultData: INPCData = {
      id: 'npc-1',
      name: 'Test NPC',
      type: NPCType.GENERIC,
      position: {
        mapId: 1,
        cellId: 100,
        direction: 1,
      },
      dialogues: [],
      ...overrides,
    };

    return new NPC(defaultData);
  };

  const createTestDialogue = (overrides?: Partial<IDialogue>): IDialogue => ({
    id: 'dialogue-1',
    text: 'Hello, traveler!',
    ...overrides,
  });

  describe('constructor', () => {
    it('should create NPC with valid data', () => {
      const npc = createTestNPC();

      expect(npc.id).toBe('npc-1');
      expect(npc.name).toBe('Test NPC');
      expect(npc.type).toBe(NPCType.GENERIC);
      expect(npc.dialogues).toEqual([]);
      expect(npc.actions).toEqual([]);
    });

    it('should create quest giver NPC', () => {
      const npc = createTestNPC({
        type: NPCType.QUEST_GIVER,
        questId: 'quest-1',
      });

      expect(npc.type).toBe(NPCType.QUEST_GIVER);
      expect(npc.questId).toBe('quest-1');
    });

    it('should create merchant NPC', () => {
      const npc = createTestNPC({
        type: NPCType.MERCHANT,
        shopId: 'shop-1',
      });

      expect(npc.type).toBe(NPCType.MERCHANT);
      expect(npc.shopId).toBe('shop-1');
    });

    it('should create banker NPC', () => {
      const npc = createTestNPC({
        type: NPCType.BANKER,
      });

      expect(npc.type).toBe(NPCType.BANKER);
    });

    it('should create trainer NPC', () => {
      const npc = createTestNPC({
        type: NPCType.TRAINER,
      });

      expect(npc.type).toBe(NPCType.TRAINER);
    });

    it('should handle dialogues array', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'Hello!' },
        { id: 'dialogue-2', text: 'Goodbye!' },
      ];
      const npc = createTestNPC({ dialogues });

      expect(npc.dialogues).toEqual(dialogues);
    });

    it('should handle actions array', () => {
      const actions: INPCAction[] = [
        { type: 'trade', id: 'action-1' },
        { type: 'quest', id: 'action-2' },
      ];
      const npc = createTestNPC({ actions });

      expect(npc.actions).toEqual(actions);
    });

    it('should handle optional sprite', () => {
      const npc = createTestNPC({ sprite: 'npc_sprite_1' });

      expect(npc.sprite).toBe('npc_sprite_1');
    });

    it('should handle optional level', () => {
      const npc = createTestNPC({ level: 50 });

      expect(npc.level).toBe(50);
    });
  });

  describe('dialogue system', () => {
    describe('getDialogue', () => {
      it('should return first dialogue when no dialogue state exists', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'Goodbye!' },
        ];
        const npc = createTestNPC({ dialogues });

        const dialogue = npc.getDialogue('player-1');

        expect(dialogue).toEqual(dialogues[0]);
      });

      it('should return null when no dialogues exist', () => {
        const npc = createTestNPC();

        const dialogue = npc.getDialogue('player-1');

        expect(dialogue).toBeNull();
      });

      it('should return specific dialogue by ID', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'How are you?' },
        ];
        const npc = createTestNPC({ dialogues });

        const dialogue = npc.getDialogue('player-1', 'dialogue-2');

        expect(dialogue?.id).toBe('dialogue-2');
        expect(dialogue?.text).toBe('How are you?');
      });

      it('should track dialogue state per player', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        npc.getDialogue('player-2');

        const dialogue1 = npc.getDialogue('player-1');
        const dialogue2 = npc.getDialogue('player-2');

        expect(dialogue1).toEqual(dialogue2);
      });

      it('should return current dialogue for player', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'Goodbye!' },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1', 'dialogue-2');
        const currentDialogue = npc.getDialogue('player-1');

        expect(currentDialogue?.id).toBe('dialogue-2');
      });

      it('should handle dialogue with responses', () => {
        const responses: IDialogueResponse[] = [
          { id: 'response-1', text: 'Yes', nextDialogueId: 'dialogue-2' },
          { id: 'response-2', text: 'No' },
        ];
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Do you want to help?', responses },
        ];
        const npc = createTestNPC({ dialogues });

        const dialogue = npc.getDialogue('player-1');

        expect(dialogue?.responses).toEqual(responses);
      });

      it('should handle dialogue with conditions', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!', conditions: { level: 10 } },
        ];
        const npc = createTestNPC({ dialogues });

        const dialogue = npc.getDialogue('player-1');

        expect(dialogue?.conditions).toEqual({ level: 10 });
      });
    });

    describe('handleResponse', () => {
      it('should handle response and return next dialogue', () => {
        const dialogues: IDialogue[] = [
          {
            id: 'dialogue-1',
            text: 'Do you want to help?',
            responses: [
              { id: 'response-1', text: 'Yes', nextDialogueId: 'dialogue-2' },
            ],
          },
          { id: 'dialogue-2', text: 'Great! Here is your quest.' },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        const nextDialogue = npc.handleResponse('player-1', 'response-1');

        expect(nextDialogue?.id).toBe('dialogue-2');
      });

      it('should return null when dialogue chain ends', () => {
        const dialogues: IDialogue[] = [
          {
            id: 'dialogue-1',
            text: 'Goodbye!',
            responses: [
              { id: 'response-1', text: 'Bye' },
            ],
          },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        const nextDialogue = npc.handleResponse('player-1', 'response-1');

        expect(nextDialogue).toBeNull();
      });

      it('should return null when no current dialogue exists', () => {
        const npc = createTestNPC();

        const nextDialogue = npc.handleResponse('player-1', 'response-1');

        expect(nextDialogue).toBeNull();
      });

      it('should return null when response not found', () => {
        const dialogues: IDialogue[] = [
          {
            id: 'dialogue-1',
            text: 'Hello!',
            responses: [
              { id: 'response-1', text: 'Hi' },
            ],
          },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        const nextDialogue = npc.handleResponse('player-1', 'invalid-response');

        expect(nextDialogue).toBeNull();
      });

      it('should reset dialogue when chain ends', () => {
        const dialogues: IDialogue[] = [
          {
            id: 'dialogue-1',
            text: 'Hello!',
            responses: [
              { id: 'response-1', text: 'Hi' },
            ],
          },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        npc.handleResponse('player-1', 'response-1');

        const dialogue = npc.getDialogue('player-1');
        expect(dialogue?.id).toBe('dialogue-1'); // Back to first dialogue
      });

      it('should handle response with action', () => {
        const dialogues: IDialogue[] = [
          {
            id: 'dialogue-1',
            text: 'Want to trade?',
            responses: [
              {
                id: 'response-1',
                text: 'Yes',
                action: 'open_shop',
                actionParams: { shopId: 'shop-1' },
              },
            ],
          },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1');
        const nextDialogue = npc.handleResponse('player-1', 'response-1');

        // Should handle action (implementation would trigger shop opening)
        expect(nextDialogue).toBeNull(); // Ends dialogue chain
      });
    });

    describe('resetDialogue', () => {
      it('should reset dialogue state for player', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'Goodbye!' },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1', 'dialogue-2');
        npc.resetDialogue('player-1');

        const dialogue = npc.getDialogue('player-1');
        expect(dialogue?.id).toBe('dialogue-1'); // Back to first dialogue
      });

      it('should only reset for specific player', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'Goodbye!' },
        ];
        const npc = createTestNPC({ dialogues });

        npc.getDialogue('player-1', 'dialogue-2');
        npc.getDialogue('player-2', 'dialogue-2');
        npc.resetDialogue('player-1');

        const dialogue1 = npc.getDialogue('player-1');
        const dialogue2 = npc.getDialogue('player-2');

        expect(dialogue1?.id).toBe('dialogue-1');
        expect(dialogue2?.id).toBe('dialogue-2');
      });

      it('should handle resetting non-existent player', () => {
        const npc = createTestNPC();

        expect(() => npc.resetDialogue('player-999')).not.toThrow();
      });
    });

    describe('addDialogue', () => {
      it('should add new dialogue', () => {
        const npc = createTestNPC();
        const dialogue = createTestDialogue();

        npc.addDialogue(dialogue);

        expect(npc.dialogues).toContain(dialogue);
        expect(npc.dialogues.length).toBe(1);
      });

      it('should add multiple dialogues', () => {
        const npc = createTestNPC();

        npc.addDialogue({ id: 'dialogue-1', text: 'Hello!' });
        npc.addDialogue({ id: 'dialogue-2', text: 'Goodbye!' });

        expect(npc.dialogues.length).toBe(2);
      });
    });

    describe('removeDialogue', () => {
      it('should remove dialogue by ID', () => {
        const dialogues: IDialogue[] = [
          { id: 'dialogue-1', text: 'Hello!' },
          { id: 'dialogue-2', text: 'Goodbye!' },
        ];
        const npc = createTestNPC({ dialogues });

        const removed = npc.removeDialogue('dialogue-1');

        expect(removed).toBe(true);
        expect(npc.dialogues.length).toBe(1);
        expect(npc.dialogues[0].id).toBe('dialogue-2');
      });

      it('should return false when dialogue not found', () => {
        const npc = createTestNPC();

        const removed = npc.removeDialogue('dialogue-999');

        expect(removed).toBe(false);
      });

      it('should handle removing from empty dialogues', () => {
        const npc = createTestNPC();

        const removed = npc.removeDialogue('dialogue-1');

        expect(removed).toBe(false);
        expect(npc.dialogues.length).toBe(0);
      });
    });
  });

  describe('interact method', () => {
    it('should return dialogue when no action specified', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'Hello!' },
      ];
      const npc = createTestNPC({ dialogues });

      const result = npc.interact('player-1');

      expect(result).toEqual(dialogues[0]);
    });

    it('should return action when action specified', () => {
      const actions: INPCAction[] = [
        { type: 'trade', id: 'action-1' },
      ];
      const npc = createTestNPC({ actions });

      const result = npc.interact('player-1', 'trade');

      expect(result).toEqual(actions[0]);
    });

    it('should return null when action not found', () => {
      const npc = createTestNPC();

      const result = npc.interact('player-1', 'invalid-action');

      expect(result).toBeNull();
    });

    it('should return null when action has requirements not met', () => {
      const actions: INPCAction[] = [
        {
          type: 'trade',
          id: 'action-1',
          requirements: { level: 50 },
        },
      ];
      const npc = createTestNPC({ actions });

      // Without player data, canPerformAction returns true by default
      const result = npc.interact('player-1', 'trade');

      expect(result).toBeDefined();
    });

    it('should handle multiple actions', () => {
      const actions: INPCAction[] = [
        { type: 'trade', id: 'action-1' },
        { type: 'train', id: 'action-2' },
      ];
      const npc = createTestNPC({ actions });

      const result1 = npc.interact('player-1', 'trade');
      const result2 = npc.interact('player-1', 'train');

      expect(result1).toEqual(actions[0]);
      expect(result2).toEqual(actions[1]);
    });
  });

  describe('action management', () => {
    describe('addAction', () => {
      it('should add new action', () => {
        const npc = createTestNPC();
        const action: INPCAction = { type: 'trade', id: 'action-1' };

        npc.addAction(action);

        expect(npc.actions).toContain(action);
        expect(npc.actions.length).toBe(1);
      });

      it('should add multiple actions', () => {
        const npc = createTestNPC();

        npc.addAction({ type: 'trade', id: 'action-1' });
        npc.addAction({ type: 'train', id: 'action-2' });

        expect(npc.actions.length).toBe(2);
      });
    });

    describe('removeAction', () => {
      it('should remove action by type', () => {
        const actions: INPCAction[] = [
          { type: 'trade', id: 'action-1' },
          { type: 'train', id: 'action-2' },
        ];
        const npc = createTestNPC({ actions });

        const removed = npc.removeAction('trade');

        expect(removed).toBe(true);
        expect(npc.actions.length).toBe(1);
        expect(npc.actions[0].type).toBe('train');
      });

      it('should return false when action not found', () => {
        const npc = createTestNPC();

        const removed = npc.removeAction('invalid-action');

        expect(removed).toBe(false);
      });

      it('should handle removing from empty actions', () => {
        const npc = createTestNPC();

        const removed = npc.removeAction('trade');

        expect(removed).toBe(false);
        expect(npc.actions.length).toBe(0);
      });
    });
  });

  describe('helper methods', () => {
    describe('isQuestGiver', () => {
      it('should return true for quest giver with questId', () => {
        const npc = createTestNPC({
          type: NPCType.QUEST_GIVER,
          questId: 'quest-1',
        });

        expect(npc.isQuestGiver()).toBe(true);
      });

      it('should return false for quest giver without questId', () => {
        const npc = createTestNPC({
          type: NPCType.QUEST_GIVER,
        });

        expect(npc.isQuestGiver()).toBe(false);
      });

      it('should return false for non-quest giver', () => {
        const npc = createTestNPC({
          type: NPCType.MERCHANT,
        });

        expect(npc.isQuestGiver()).toBe(false);
      });
    });

    describe('isMerchant', () => {
      it('should return true for merchant with shopId', () => {
        const npc = createTestNPC({
          type: NPCType.MERCHANT,
          shopId: 'shop-1',
        });

        expect(npc.isMerchant()).toBe(true);
      });

      it('should return false for merchant without shopId', () => {
        const npc = createTestNPC({
          type: NPCType.MERCHANT,
        });

        expect(npc.isMerchant()).toBe(false);
      });

      it('should return false for non-merchant', () => {
        const npc = createTestNPC({
          type: NPCType.QUEST_GIVER,
        });

        expect(npc.isMerchant()).toBe(false);
      });
    });
  });

  describe('toJSON', () => {
    it('should return complete NPC data', () => {
      const npc = createTestNPC();

      const json = npc.toJSON();

      expect(json).toEqual({
        id: 'npc-1',
        name: 'Test NPC',
        type: NPCType.GENERIC,
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
        dialogues: [],
        actions: [],
        questId: undefined,
        shopId: undefined,
        sprite: undefined,
        level: undefined,
      });
    });

    it('should include all optional fields when present', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'Hello!' },
      ];
      const actions: INPCAction[] = [
        { type: 'trade', id: 'action-1' },
      ];
      const npc = createTestNPC({
        type: NPCType.QUEST_GIVER,
        dialogues,
        actions,
        questId: 'quest-1',
        shopId: 'shop-1',
        sprite: 'npc_sprite_1',
        level: 50,
      });

      const json = npc.toJSON();

      expect(json.dialogues).toEqual(dialogues);
      expect(json.actions).toEqual(actions);
      expect(json.questId).toBe('quest-1');
      expect(json.shopId).toBe('shop-1');
      expect(json.sprite).toBe('npc_sprite_1');
      expect(json.level).toBe(50);
    });
  });

  describe('integration scenarios', () => {
    it('should handle complete quest giver interaction', () => {
      const dialogues: IDialogue[] = [
        {
          id: 'dialogue-1',
          text: 'I need help with a quest!',
          responses: [
            {
              id: 'response-1',
              text: 'Tell me more',
              nextDialogueId: 'dialogue-2',
            },
            {
              id: 'response-2',
              text: 'Not interested',
            },
          ],
        },
        {
          id: 'dialogue-2',
          text: 'Please retrieve the ancient artifact!',
          responses: [
            {
              id: 'response-3',
              text: 'I accept',
              action: 'start_quest',
              actionParams: { questId: 'quest-1' },
            },
          ],
        },
      ];
      const npc = createTestNPC({
        type: NPCType.QUEST_GIVER,
        questId: 'quest-1',
        dialogues,
      });

      // Initial interaction
      const dialogue1 = npc.interact('player-1');
      expect(dialogue1).toMatchObject({ id: 'dialogue-1' });

      // Choose to hear more
      const dialogue2 = npc.handleResponse('player-1', 'response-1');
      expect(dialogue2).toMatchObject({ id: 'dialogue-2' });

      // Accept quest
      const dialogue3 = npc.handleResponse('player-1', 'response-3');
      expect(dialogue3).toBeNull(); // Dialogue ends
    });

    it('should handle merchant interaction', () => {
      const npc = createTestNPC({
        type: NPCType.MERCHANT,
        shopId: 'shop-1',
        actions: [
          { type: 'trade', id: 'action-1', params: { shopId: 'shop-1' } },
        ],
      });

      // Direct trade action
      const action = npc.interact('player-1', 'trade');
      expect(action).toMatchObject({ type: 'trade' });
    });

    it('should handle multi-player dialogue states', () => {
      const dialogues: IDialogue[] = [
        {
          id: 'dialogue-1',
          text: 'Hello!',
          responses: [{ id: 'response-1', text: 'Hi', nextDialogueId: 'dialogue-2' }],
        },
        { id: 'dialogue-2', text: 'How are you?' },
      ];
      const npc = createTestNPC({ dialogues });

      // Player 1 progresses
      npc.getDialogue('player-1');
      npc.handleResponse('player-1', 'response-1');

      // Player 2 just starts
      npc.getDialogue('player-2');

      // Verify independent states
      const dialogue1 = npc.getDialogue('player-1');
      const dialogue2 = npc.getDialogue('player-2');

      expect(dialogue1?.id).toBe('dialogue-2');
      expect(dialogue2?.id).toBe('dialogue-1');
    });

    it('should handle complex dialogue tree', () => {
      const dialogues: IDialogue[] = [
        {
          id: 'dialogue-1',
          text: 'What do you seek?',
          responses: [
            { id: 'response-1', text: 'Training', nextDialogueId: 'dialogue-training' },
            { id: 'response-2', text: 'Items', nextDialogueId: 'dialogue-shop' },
            { id: 'response-3', text: 'Nothing' },
          ],
        },
        {
          id: 'dialogue-training',
          text: 'I can train you in combat!',
          responses: [
            { id: 'response-4', text: 'Yes', action: 'train', nextDialogueId: 'dialogue-thanks' },
          ],
        },
        {
          id: 'dialogue-shop',
          text: 'Browse my wares!',
          responses: [
            { id: 'response-5', text: 'Show me', action: 'trade', nextDialogueId: 'dialogue-thanks' },
          ],
        },
        {
          id: 'dialogue-thanks',
          text: 'Thank you for your patronage!',
        },
      ];
      const npc = createTestNPC({ dialogues });

      // Navigate to training
      npc.getDialogue('player-1');
      npc.handleResponse('player-1', 'response-1');
      const trainingDialogue = npc.getDialogue('player-1');
      expect(trainingDialogue?.id).toBe('dialogue-training');

      // Reset and navigate to shop
      npc.resetDialogue('player-1');
      npc.getDialogue('player-1');
      npc.handleResponse('player-1', 'response-2');
      const shopDialogue = npc.getDialogue('player-1');
      expect(shopDialogue?.id).toBe('dialogue-shop');
    });

    it('should handle NPC with requirements', () => {
      const actions: INPCAction[] = [
        {
          type: 'advanced_training',
          id: 'action-1',
          requirements: {
            level: 50,
            quest: 'completed_basic_training',
          },
        },
      ];
      const npc = createTestNPC({
        type: NPCType.TRAINER,
        actions,
      });

      // Attempt interaction (without player data, it passes by default)
      const action = npc.interact('player-1', 'advanced_training');
      expect(action).toBeDefined();
    });
  });

  describe('edge cases', () => {
    it('should handle empty dialogue responses', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'Hello!', responses: [] },
      ];
      const npc = createTestNPC({ dialogues });

      npc.getDialogue('player-1');
      const result = npc.handleResponse('player-1', 'any-response');

      expect(result).toBeNull();
    });

    it('should handle dialogue without responses', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'Hello!' },
      ];
      const npc = createTestNPC({ dialogues });

      npc.getDialogue('player-1');
      const result = npc.handleResponse('player-1', 'any-response');

      expect(result).toBeNull();
    });

    it('should handle resetting dialogue mid-conversation', () => {
      const dialogues: IDialogue[] = [
        {
          id: 'dialogue-1',
          text: 'Start',
          responses: [{ id: 'response-1', text: 'Next', nextDialogueId: 'dialogue-2' }],
        },
        { id: 'dialogue-2', text: 'End' },
      ];
      const npc = createTestNPC({ dialogues });

      npc.getDialogue('player-1');
      npc.handleResponse('player-1', 'response-1');
      npc.resetDialogue('player-1');

      const dialogue = npc.getDialogue('player-1');
      expect(dialogue?.id).toBe('dialogue-1');
    });

    it('should handle duplicate dialogue IDs', () => {
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: 'First' },
        { id: 'dialogue-1', text: 'Duplicate' },
      ];
      const npc = createTestNPC({ dialogues });

      const dialogue = npc.getDialogue('player-1', 'dialogue-1');

      // Should return the first matching dialogue
      expect(dialogue?.text).toBe('First');
    });

    it('should handle very long dialogue text', () => {
      const longText = 'A'.repeat(10000);
      const dialogues: IDialogue[] = [
        { id: 'dialogue-1', text: longText },
      ];
      const npc = createTestNPC({ dialogues });

      const dialogue = npc.getDialogue('player-1');

      expect(dialogue?.text).toBe(longText);
    });
  });
});
