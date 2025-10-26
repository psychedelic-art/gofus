/**
 * Entity exports
 * Central export point for all game entities
 */

// Base Entity
export { Entity, IPosition, IEntityData } from './Entity';

// Player
export {
  Player,
  IPlayerData,
  IPlayerStats,
  IPlayerCharacteristics,
} from './Player';

// NPC
export {
  NPC,
  NPCType,
  INPCData,
  IDialogue,
  IDialogueResponse,
  INPCAction,
} from './NPC';

// Monster
export {
  Monster,
  IMonsterData,
  IMonsterStats,
  IMonsterDrop,
} from './Monster';

// GameObject
export {
  GameObject,
  GameObjectType,
  IGameObjectData,
  IGameObjectRequirement,
  IGameObjectReward,
} from './GameObject';
