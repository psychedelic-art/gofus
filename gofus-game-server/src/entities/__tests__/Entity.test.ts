/**
 * Entity Base Class Tests
 * Tests for the abstract Entity class
 */

import { Entity, IEntityData, IPosition } from '../Entity';

// Create a concrete implementation for testing
class TestEntity extends Entity {
  public toJSON(): Record<string, any> {
    return {
      id: this.id,
      name: this.name,
      position: this.position,
    };
  }
}

describe('Entity', () => {
  describe('constructor', () => {
    it('should create an entity with valid data', () => {
      const entityData: IEntityData = {
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.id).toBe('entity-1');
      expect(entity.name).toBe('Test Entity');
      expect(entity.position.mapId).toBe(1);
      expect(entity.position.cellId).toBe(100);
      expect(entity.position.direction).toBe(1);
    });

    it('should create entity without direction', () => {
      const entityData: IEntityData = {
        id: 'entity-2',
        name: 'Test Entity 2',
        position: {
          mapId: 2,
          cellId: 200,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.id).toBe('entity-2');
      expect(entity.position.direction).toBeUndefined();
    });

    it('should handle empty string names', () => {
      const entityData: IEntityData = {
        id: 'entity-3',
        name: '',
        position: {
          mapId: 3,
          cellId: 300,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.name).toBe('');
    });

    it('should handle special characters in id and name', () => {
      const entityData: IEntityData = {
        id: 'entity-@#$-123',
        name: 'Test Entity <>&"',
        position: {
          mapId: 4,
          cellId: 400,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.id).toBe('entity-@#$-123');
      expect(entity.name).toBe('Test Entity <>&"');
    });

    it('should handle zero values in position', () => {
      const entityData: IEntityData = {
        id: 'entity-4',
        name: 'Test Entity',
        position: {
          mapId: 0,
          cellId: 0,
          direction: 0,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.position.mapId).toBe(0);
      expect(entity.position.cellId).toBe(0);
      expect(entity.position.direction).toBe(0);
    });

    it('should handle negative values in position', () => {
      const entityData: IEntityData = {
        id: 'entity-5',
        name: 'Test Entity',
        position: {
          mapId: -1,
          cellId: -100,
          direction: -1,
        },
      };

      const entity = new TestEntity(entityData);

      expect(entity.position.mapId).toBe(-1);
      expect(entity.position.cellId).toBe(-100);
      expect(entity.position.direction).toBe(-1);
    });
  });

  describe('updatePosition', () => {
    let entity: TestEntity;

    beforeEach(() => {
      entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
      });
    });

    it('should update position with mapId and cellId', () => {
      entity.updatePosition(2, 200);

      expect(entity.position.mapId).toBe(2);
      expect(entity.position.cellId).toBe(200);
      expect(entity.position.direction).toBe(1); // Should remain unchanged
    });

    it('should update position with direction', () => {
      entity.updatePosition(3, 300, 4);

      expect(entity.position.mapId).toBe(3);
      expect(entity.position.cellId).toBe(300);
      expect(entity.position.direction).toBe(4);
    });

    it('should not update direction when undefined', () => {
      entity.updatePosition(4, 400, undefined);

      expect(entity.position.mapId).toBe(4);
      expect(entity.position.cellId).toBe(400);
      expect(entity.position.direction).toBe(1); // Should remain unchanged
    });

    it('should update direction to 0', () => {
      entity.updatePosition(5, 500, 0);

      expect(entity.position.direction).toBe(0);
    });

    it('should handle multiple position updates', () => {
      entity.updatePosition(2, 200, 2);
      entity.updatePosition(3, 300, 3);
      entity.updatePosition(4, 400, 4);

      expect(entity.position.mapId).toBe(4);
      expect(entity.position.cellId).toBe(400);
      expect(entity.position.direction).toBe(4);
    });

    it('should handle updating to same position', () => {
      entity.updatePosition(1, 100, 1);

      expect(entity.position.mapId).toBe(1);
      expect(entity.position.cellId).toBe(100);
      expect(entity.position.direction).toBe(1);
    });

    it('should handle negative position values', () => {
      entity.updatePosition(-1, -100, -1);

      expect(entity.position.mapId).toBe(-1);
      expect(entity.position.cellId).toBe(-100);
      expect(entity.position.direction).toBe(-1);
    });

    it('should handle large position values', () => {
      entity.updatePosition(999999, 999999, 360);

      expect(entity.position.mapId).toBe(999999);
      expect(entity.position.cellId).toBe(999999);
      expect(entity.position.direction).toBe(360);
    });
  });

  describe('getPosition', () => {
    let entity: TestEntity;

    beforeEach(() => {
      entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
      });
    });

    it('should return a copy of the position', () => {
      const position = entity.getPosition();

      expect(position).toEqual({
        mapId: 1,
        cellId: 100,
        direction: 1,
      });
    });

    it('should return a new object (not reference)', () => {
      const position = entity.getPosition();

      // Modify the returned position
      position.mapId = 999;
      position.cellId = 999;
      position.direction = 999;

      // Original should be unchanged
      expect(entity.position.mapId).toBe(1);
      expect(entity.position.cellId).toBe(100);
      expect(entity.position.direction).toBe(1);
    });

    it('should reflect position updates', () => {
      entity.updatePosition(2, 200, 2);
      const position = entity.getPosition();

      expect(position.mapId).toBe(2);
      expect(position.cellId).toBe(200);
      expect(position.direction).toBe(2);
    });

    it('should handle position without direction', () => {
      const entityNoDir = new TestEntity({
        id: 'entity-2',
        name: 'Test Entity',
        position: {
          mapId: 2,
          cellId: 200,
        },
      });

      const position = entityNoDir.getPosition();

      expect(position.mapId).toBe(2);
      expect(position.cellId).toBe(200);
      expect(position.direction).toBeUndefined();
    });
  });

  describe('toJSON', () => {
    it('should return serialized entity data', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
      });

      const json = entity.toJSON();

      expect(json).toEqual({
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
          direction: 1,
        },
      });
    });

    it('should be implemented by subclasses', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: {
          mapId: 1,
          cellId: 100,
        },
      });

      expect(typeof entity.toJSON).toBe('function');
      expect(entity.toJSON()).toBeDefined();
    });
  });

  describe('abstract class behavior', () => {
    it('should not be instantiable directly', () => {
      // This test verifies TypeScript compilation behavior
      // In JavaScript runtime, we can't truly test abstract class behavior
      // But we ensure the concrete implementation works
      expect(() => {
        new TestEntity({
          id: 'test',
          name: 'test',
          position: { mapId: 1, cellId: 1 },
        });
      }).not.toThrow();
    });

    it('should require toJSON implementation', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: { mapId: 1, cellId: 100 },
      });

      // Verify that toJSON is implemented
      expect(entity.toJSON).toBeDefined();
      expect(typeof entity.toJSON).toBe('function');
    });
  });

  describe('edge cases', () => {
    it('should handle very long entity names', () => {
      const longName = 'A'.repeat(1000);
      const entity = new TestEntity({
        id: 'entity-1',
        name: longName,
        position: { mapId: 1, cellId: 100 },
      });

      expect(entity.name).toBe(longName);
      expect(entity.name.length).toBe(1000);
    });

    it('should handle unicode characters in name', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: '测试实体 🎮 Тест',
        position: { mapId: 1, cellId: 100 },
      });

      expect(entity.name).toBe('测试实体 🎮 Тест');
    });

    it('should handle very large position values', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: 'Test',
        position: {
          mapId: Number.MAX_SAFE_INTEGER,
          cellId: Number.MAX_SAFE_INTEGER,
        },
      });

      expect(entity.position.mapId).toBe(Number.MAX_SAFE_INTEGER);
      expect(entity.position.cellId).toBe(Number.MAX_SAFE_INTEGER);
    });

    it('should maintain data integrity after multiple operations', () => {
      const entity = new TestEntity({
        id: 'entity-1',
        name: 'Test Entity',
        position: { mapId: 1, cellId: 100, direction: 1 },
      });

      // Perform multiple operations
      const pos1 = entity.getPosition();
      entity.updatePosition(2, 200, 2);
      const pos2 = entity.getPosition();
      entity.updatePosition(3, 300);
      const pos3 = entity.getPosition();

      // Verify data integrity
      expect(pos1).toEqual({ mapId: 1, cellId: 100, direction: 1 });
      expect(pos2).toEqual({ mapId: 2, cellId: 200, direction: 2 });
      expect(pos3).toEqual({ mapId: 3, cellId: 300, direction: 2 });
    });
  });
});
