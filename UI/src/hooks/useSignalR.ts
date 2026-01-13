import { useEffect, useState } from 'react';
import { HubConnectionState } from '@microsoft/signalr';
import { signalRService } from '@/services/signalr.service';
import { SignalRConnectionState } from '@/types/obs';

/**
 * Hook for managing SignalR connection state
 */
export const useSignalR = () => {
  const [connectionState, setConnectionState] = useState<SignalRConnectionState>(
    SignalRConnectionState.Disconnected
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    const initializeConnection = async () => {
      try {
        setConnectionState(SignalRConnectionState.Connecting);
        await signalRService.start();

        if (mounted) {
          setConnectionState(SignalRConnectionState.Connected);
          setError(null);
        }
      } catch (err) {
        console.error('Failed to start SignalR connection:', err);
        if (mounted) {
          setConnectionState(SignalRConnectionState.Disconnected);
          setError(err instanceof Error ? err.message : 'Failed to connect');
        }
      }
    };

    // Check connection state periodically
    const checkConnectionState = () => {
      const state = signalRService.getConnectionState();

      if (!mounted) return;

      switch (state) {
        case HubConnectionState.Connected:
          setConnectionState(SignalRConnectionState.Connected);
          setError(null);
          break;
        case HubConnectionState.Connecting:
          setConnectionState(SignalRConnectionState.Connecting);
          break;
        case HubConnectionState.Reconnecting:
          setConnectionState(SignalRConnectionState.Reconnecting);
          break;
        case HubConnectionState.Disconnecting:
          setConnectionState(SignalRConnectionState.Disconnecting);
          break;
        case HubConnectionState.Disconnected:
          setConnectionState(SignalRConnectionState.Disconnected);
          break;
      }
    };

    initializeConnection();

    // Poll connection state every 2 seconds
    const intervalId = setInterval(checkConnectionState, 2000);

    return () => {
      mounted = false;
      clearInterval(intervalId);
      // Don't stop the singleton SignalR service on component unmount
      // This prevents React StrictMode from breaking the connection during development
      // The service will maintain its connection across component lifecycles
    };
  }, []);

  return {
    connectionState,
    isConnected: connectionState === SignalRConnectionState.Connected,
    error,
  };
};

