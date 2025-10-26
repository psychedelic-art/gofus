import { log } from '@/utils/Logger';
import { SocketHandler } from '@/network/SocketHandler';

export class ChatManager {
  private socketHandler: SocketHandler;

  constructor(socketHandler: SocketHandler) {
    this.socketHandler = socketHandler;
  }

  public async initialize(): Promise<void> {
    log.info('ChatManager initialized');
  }

  public async cleanup(): Promise<void> {
    log.info('ChatManager cleaned up');
  }
}

export default ChatManager;