// Game Constants and Formulas

export const GAME_CONSTANTS = {
  // Character limits
  MAX_CHARACTERS_PER_ACCOUNT: 5,
  MAX_LEVEL: 200,
  DEFAULT_START_MAP: 7411,
  DEFAULT_START_CELL: 311,

  // Stats
  STATS_PER_LEVEL: 5,
  SPELL_POINTS_PER_LEVEL: 1,

  // Energy
  MAX_ENERGY: 10000,
  ENERGY_RECOVERY_RATE: 1, // per second

  // Movement
  DEFAULT_MOVEMENT_POINTS: 3,
  DIAGONAL_MOVEMENT_COST: 1,
  LINEAR_MOVEMENT_COST: 1,

  // Combat
  DEFAULT_ACTION_POINTS: 6,
  DEFAULT_INITIATIVE: 100,
  MAX_SUMMONS_PER_PLAYER: 1,
  CRITICAL_HIT_BASE_CHANCE: 5, // 5%
  CRITICAL_HIT_MULTIPLIER: 1.5,

  // Chat
  CHAT_MESSAGE_MAX_LENGTH: 255,
  CHAT_RATE_LIMIT: 10, // messages per minute
  PRIVATE_MESSAGE_MIN_LEVEL: 10,

  // Trade
  TRADE_TAX_RATE: 0.02, // 2%
  MIN_TRADE_LEVEL: 20,
  TRADE_TIMEOUT: 60000, // 1 minute

  // Guild
  GUILD_NAME_MIN_LENGTH: 3,
  GUILD_NAME_MAX_LENGTH: 20,
  MIN_LEVEL_CREATE_GUILD: 10,
  GUILD_CREATION_COST: 10000, // Kamas

  // Inventory
  MAX_INVENTORY_SLOTS: 100,
  MAX_STACK_SIZE: 100,
  EQUIPMENT_SLOTS: {
    HAT: 0,
    CLOAK: 1,
    AMULET: 2,
    LEFT_RING: 3,
    RIGHT_RING: 4,
    BELT: 5,
    BOOTS: 6,
    WEAPON: 7,
    SHIELD: 8,
    PET: 9,
    MOUNT: 10,
  },

  // Respawn
  RESPAWN_HP_PERCENT: 10,
  RESPAWN_ENERGY_COST: 100,
  DEATH_XP_PENALTY: 0.1, // 10% XP loss

  // Map
  MAP_WIDTH: 14,
  MAP_HEIGHT: 20,
  CELLS_PER_MAP: 560,
  VIEW_DISTANCE: 20, // cells

  // AI
  MOB_AGGRO_RANGE: 5, // cells
  MOB_RESPAWN_TIME: 300000, // 5 minutes
  MOB_MAX_PURSUIT_DISTANCE: 15,

  // Network
  PACKET_TYPES: {
    // Authentication
    AUTH_REQUEST: 'AUTH_REQUEST',
    AUTH_SUCCESS: 'AUTH_SUCCESS',
    AUTH_FAILED: 'AUTH_FAILED',

    // Movement
    MOVEMENT_REQUEST: 'MOVEMENT_REQUEST',
    MOVEMENT_UPDATE: 'MOVEMENT_UPDATE',
    MOVEMENT_INVALID: 'MOVEMENT_INVALID',

    // Combat
    COMBAT_START: 'COMBAT_START',
    COMBAT_TURN: 'COMBAT_TURN',
    COMBAT_ACTION: 'COMBAT_ACTION',
    COMBAT_END: 'COMBAT_END',

    // Chat
    CHAT_MESSAGE: 'CHAT_MESSAGE',
    CHAT_PRIVATE: 'CHAT_PRIVATE',
    CHAT_GUILD: 'CHAT_GUILD',
    CHAT_ERROR: 'CHAT_ERROR',

    // Map
    MAP_ENTER: 'MAP_ENTER',
    MAP_LEAVE: 'MAP_LEAVE',
    MAP_STATE: 'MAP_STATE',
    MAP_UPDATE: 'MAP_UPDATE',

    // Entity
    ENTITY_SPAWN: 'ENTITY_SPAWN',
    ENTITY_DESPAWN: 'ENTITY_DESPAWN',
    ENTITY_UPDATE: 'ENTITY_UPDATE',

    // System
    PING: 'PING',
    PONG: 'PONG',
    SERVER_MESSAGE: 'SERVER_MESSAGE',
    ERROR: 'ERROR',
  },
};

// Experience formulas
export const ExperienceFormulas = {
  // Calculate XP required for a level
  getExperienceForLevel(level: number): number {
    if (level <= 1) return 0;
    return Math.floor(level * level * 100);
  },

  // Get level from experience
  getLevelFromExperience(experience: number): number {
    return Math.floor(Math.sqrt(experience / 100));
  },

  // Calculate XP reward
  calculateXPReward(
    playerLevel: number,
    monsterLevel: number,
    baseXP: number
  ): number {
    const levelDiff = Math.abs(playerLevel - monsterLevel);
    let multiplier = 1;

    if (levelDiff > 10) {
      multiplier = Math.max(0.1, 1 - (levelDiff - 10) * 0.1);
    }

    return Math.floor(baseXP * multiplier);
  },
};

// Combat formulas
export const CombatFormulas = {
  // Calculate damage
  calculateDamage(
    baseDamage: number,
    attackerStats: any,
    defenderStats: any,
    element: string
  ): number {
    // Base damage with stat modifiers
    let damage = baseDamage;

    // Apply elemental bonuses
    switch (element) {
      case 'fire':
        damage += attackerStats.intelligence || 0;
        damage -= defenderStats.fireResistance || 0;
        break;
      case 'water':
        damage += attackerStats.chance || 0;
        damage -= defenderStats.waterResistance || 0;
        break;
      case 'earth':
        damage += attackerStats.strength || 0;
        damage -= defenderStats.earthResistance || 0;
        break;
      case 'air':
        damage += attackerStats.agility || 0;
        damage -= defenderStats.airResistance || 0;
        break;
      case 'neutral':
        damage += (attackerStats.strength || 0) / 2;
        damage -= defenderStats.neutralResistance || 0;
        break;
    }

    return Math.max(1, Math.floor(damage));
  },

  // Calculate initiative
  calculateInitiative(stats: any): number {
    const baseInitiative = GAME_CONSTANTS.DEFAULT_INITIATIVE;
    const agilityBonus = (stats.agility || 0) * 2;
    const intelligenceBonus = (stats.intelligence || 0);

    return baseInitiative + agilityBonus + intelligenceBonus;
  },

  // Calculate critical hit chance
  calculateCriticalChance(stats: any, weaponCritChance: number = 0): number {
    const baseChance = GAME_CONSTANTS.CRITICAL_HIT_BASE_CHANCE;
    const agilityBonus = Math.floor((stats.agility || 0) / 10);

    return Math.min(100, baseChance + agilityBonus + weaponCritChance);
  },

  // Calculate dodge chance
  calculateDodgeChance(stats: any): number {
    const agilityBonus = Math.floor((stats.agility || 0) / 4);
    return Math.min(50, agilityBonus);
  },

  // Calculate heal amount
  calculateHeal(baseHeal: number, casterStats: any): number {
    const intelligenceBonus = (casterStats.intelligence || 0) * 0.5;
    return Math.floor(baseHeal + intelligenceBonus);
  },
};

// Movement formulas
export const MovementFormulas = {
  // Calculate movement points
  calculateMovementPoints(stats: any): number {
    const baseMP = GAME_CONSTANTS.DEFAULT_MOVEMENT_POINTS;
    const agilityBonus = Math.floor((stats.agility || 0) / 100);

    return baseMP + agilityBonus;
  },

  // Calculate distance between cells
  calculateDistance(cell1: number, cell2: number): number {
    const mapWidth = GAME_CONSTANTS.MAP_WIDTH;
    const x1 = cell1 % mapWidth;
    const y1 = Math.floor(cell1 / mapWidth);
    const x2 = cell2 % mapWidth;
    const y2 = Math.floor(cell2 / mapWidth);

    return Math.abs(x1 - x2) + Math.abs(y1 - y2);
  },

  // Check line of sight
  hasLineOfSight(
    fromCell: number,
    toCell: number,
    obstacles: Set<number>
  ): boolean {
    const mapWidth = GAME_CONSTANTS.MAP_WIDTH;
    const x1 = fromCell % mapWidth;
    const y1 = Math.floor(fromCell / mapWidth);
    const x2 = toCell % mapWidth;
    const y2 = Math.floor(toCell / mapWidth);

    const dx = Math.abs(x2 - x1);
    const dy = Math.abs(y2 - y1);
    const sx = x1 < x2 ? 1 : -1;
    const sy = y1 < y2 ? 1 : -1;

    let err = dx - dy;
    let x = x1;
    let y = y1;

    while (x !== x2 || y !== y2) {
      const cell = y * mapWidth + x;
      if (obstacles.has(cell)) {
        return false;
      }

      const e2 = 2 * err;
      if (e2 > -dy) {
        err -= dy;
        x += sx;
      }
      if (e2 < dx) {
        err += dx;
        y += sy;
      }
    }

    return true;
  },
};

// Item formulas
export const ItemFormulas = {
  // Calculate item power
  calculateItemPower(item: any): number {
    let power = item.level || 0;

    // Add stat bonuses
    if (item.stats) {
      Object.values(item.stats).forEach((value: any) => {
        power += Math.abs(Number(value) || 0);
      });
    }

    return power;
  },

  // Calculate item price
  calculateItemPrice(item: any, condition: number = 100): number {
    const basePrice = item.price || 0;
    const conditionMultiplier = condition / 100;
    const powerMultiplier = 1 + (ItemFormulas.calculateItemPower(item) / 100);

    return Math.floor(basePrice * conditionMultiplier * powerMultiplier);
  },
};

export default {
  CONSTANTS: GAME_CONSTANTS,
  Experience: ExperienceFormulas,
  Combat: CombatFormulas,
  Movement: MovementFormulas,
  Items: ItemFormulas,
};