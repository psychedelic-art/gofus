import { log } from '@/utils/Logger';
import { MapManager } from './MapManager';
import { CombatManager } from './CombatManager';

export class AIManager {
  private mapManager?: MapManager;
  private combatManager?: CombatManager;

  constructor() {}

  public async initialize(mapManager: MapManager, combatManager: CombatManager): Promise<void> {
    this.mapManager = mapManager;
    this.combatManager = combatManager;
    log.info('AIManager initialized');
  }

  public update(): void {
    // Update AI entities
  }

  public async cleanup(): Promise<void> {
    log.info('AIManager cleaned up');
  }
}

export default AIManager;