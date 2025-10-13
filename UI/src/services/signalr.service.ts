import * as signalR from '@microsoft/signalr';
import type { OBSConnectionStatus, Scene } from '@/types/obs';

/**
 * SignalR service for real-time communication with the backend
 */
class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 10;
  private reconnectDelay = 5000; // 5 seconds

  /**
   * Initialize and start the SignalR connection
   */
  public async start(): Promise<void> {
    if (this.connection) {
      console.log('SignalR connection already exists');
      return;
    }

    // Use absolute URL to connect directly to backend, bypassing Vite proxy
    const backendUrl = 'http://localhost:5080';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${backendUrl}/hubs/obs`, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          if (retryContext.previousRetryCount >= this.maxReconnectAttempts) {
            console.error('Max reconnect attempts reached');
            return null; // Stop reconnecting
          }
          return this.reconnectDelay;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // Set up connection event handlers
    this.connection.onclose((error) => {
      console.log('SignalR connection closed', error);
      this.reconnectAttempts = 0;
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
      this.reconnectAttempts++;
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected', connectionId);
      this.reconnectAttempts = 0;
    });

    try {
      await this.connection.start();
      console.log('SignalR connection started successfully');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      throw error;
    }
  }

  /**
   * Stop the SignalR connection
   */
  public async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      } finally {
        this.connection = null;
      }
    }
  }

  /**
   * Get the current connection state
   */
  public getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  /**
   * Subscribe to OBS connection status changes
   */
  public onConnectionStatusChanged(callback: (status: OBSConnectionStatus) => void): void {
    if (!this.connection) {
      console.warn('[SignalR] Connection not initialized');
      return;
    }

    console.log('[SignalR] Subscribing to ConnectionStatusChanged event');
    this.connection.on('ConnectionStatusChanged', (status: OBSConnectionStatus) => {
      console.log('[SignalR] Received ConnectionStatusChanged event:', status);
      callback(status);
    });
  }

  /**
   * Subscribe to OBS scene changes
   */
  public onSceneChanged(callback: (sceneName: string) => void): void {
    if (!this.connection) {
      console.warn('SignalR connection not initialized');
      return;
    }

    this.connection.on('SceneChanged', callback);
  }

  /**
   * Subscribe to streaming status changes
   */
  public onStreamingStatusChanged(callback: (isStreaming: boolean) => void): void {
    if (!this.connection) {
      console.warn('SignalR connection not initialized');
      return;
    }

    this.connection.on('StreamingStatusChanged', callback);
  }

  /**
   * Subscribe to a generic event
   */
  public on(eventName: string, callback: (...args: any[]) => void): void {
    if (!this.connection) {
      console.warn('SignalR connection not initialized');
      return;
    }

    this.connection.on(eventName, callback);
  }

  /**
   * Unsubscribe from an event
   */
  public off(eventName: string, callback?: (...args: any[]) => void): void {
    if (!this.connection) {
      console.warn('SignalR connection not initialized');
      return;
    }

    if (callback) {
      this.connection.off(eventName, callback);
    } else {
      this.connection.off(eventName);
    }
  }

  /**
   * Check if the connection is active
   */
  public isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Invoke a hub method
   */
  public async invoke<T = any>(methodName: string, ...args: any[]): Promise<T> {
    if (!this.connection) {
      throw new Error('SignalR connection not initialized');
    }

    return await this.connection.invoke<T>(methodName, ...args);
  }
}

// Export a singleton instance
export const signalRService = new SignalRService();

