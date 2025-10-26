/**
 * Base Entity class
 * Provides common properties and methods for all game entities
 */

export interface IPosition {
  mapId: number;
  cellId: number;
  direction?: number;
}

export interface IEntityData {
  id: string;
  name: string;
  position: IPosition;
}

/**
 * Base Entity class that all game entities extend
 */
export abstract class Entity {
  public id: string;
  public name: string;
  public position: IPosition;

  constructor(data: IEntityData) {
    this.id = data.id;
    this.name = data.name;
    this.position = data.position;
  }

  /**
   * Update the entity's position
   */
  public updatePosition(mapId: number, cellId: number, direction?: number): void {
    this.position.mapId = mapId;
    this.position.cellId = cellId;
    if (direction !== undefined) {
      this.position.direction = direction;
    }
  }

  /**
   * Get the current position
   */
  public getPosition(): IPosition {
    return { ...this.position };
  }

  /**
   * Get entity data for serialization
   */
  public abstract toJSON(): Record<string, any>;
}
