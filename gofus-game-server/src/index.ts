#!/usr/bin/env node

// Don't import dotenv here - it's loaded conditionally in server.config.ts
import { GameServer } from '@/core/GameServer';
import { log } from '@/utils/Logger';

// ASCII Banner
const banner = `
╔═══════════════════════════════════════════════════════════════╗
║                                                               ║
║   ██████╗  ██████╗ ███████╗██╗   ██╗███████╗                ║
║  ██╔════╝ ██╔═══██╗██╔════╝██║   ██║██╔════╝                ║
║  ██║  ███╗██║   ██║█████╗  ██║   ██║███████╗                ║
║  ██║   ██║██║   ██║██╔══╝  ██║   ██║╚════██║                ║
║  ╚██████╔╝╚██████╔╝██║     ╚██████╔╝███████║                ║
║   ╚═════╝  ╚═════╝ ╚═╝      ╚═════╝ ╚══════╝                ║
║                                                               ║
║              STATEFUL GAME SERVER v1.0.0                     ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
`;

async function main() {
  console.log(banner);

  try {
    // Create and start game server
    const gameServer = new GameServer();

    // Make server globally available for debugging (optional)
    if (process.env.NODE_ENV === 'development') {
      (global as any).gameServer = gameServer;
    }

    await gameServer.start();

    // Log initial server metrics
    setInterval(() => {
      const metrics = gameServer.getMetrics();
      log.info('Server Metrics', {
        uptime: Math.floor(metrics.uptime),
        players: metrics.onlinePlayers,
        maps: metrics.activeMapInstances,
        battles: metrics.activeBattles,
        memory: Math.round(metrics.memoryUsage.heapUsed / 1024 / 1024) + 'MB',
        tickDuration: metrics.lastTickDuration + 'ms',
      });
    }, 60000); // Every minute

  } catch (error) {
    log.exception(error as Error, 'Failed to start game server');
    process.exit(1);
  }
}

// Handle promise rejections
process.on('unhandledRejection', (reason, promise) => {
  log.error('Unhandled Promise Rejection', { reason, promise });
});

// Start the server
main().catch((error) => {
  console.error('Fatal error:', error);
  process.exit(1);
});