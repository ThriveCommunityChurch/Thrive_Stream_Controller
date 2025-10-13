import { useEffect, useState } from 'react';
import { signalRService } from '@/services/signalr.service';
import type { OBSConnectionStatus } from '@/types/obs';

/**
 * Hook for listening to OBS connection and scene change events via SignalR
 */
export const useOBSConnection = () => {
  const [obsStatus, setObsStatus] = useState<OBSConnectionStatus>({
    IsConnected: false,
  });
  const [currentScene, setCurrentScene] = useState<string>('');
  const [isStreaming, setIsStreaming] = useState<boolean>(false);

  useEffect(() => {
    // Handler for connection status changes
    const handleConnectionStatusChanged = (status: OBSConnectionStatus) => {
      console.log('[useOBSConnection] OBS connection status changed:', status);
      console.log('[useOBSConnection] isConnected:', status.IsConnected);
      setObsStatus(status);
    };

    // Handler for scene changes
    const handleSceneChanged = (sceneName: string) => {
      console.log('[useOBSConnection] OBS scene changed:', sceneName);
      setCurrentScene(sceneName);
    };

    // Handler for streaming status changes
    const handleStreamingStatusChanged = (streaming: boolean) => {
      console.log('[useOBSConnection] OBS streaming status changed:', streaming);
      setIsStreaming(streaming);
    };

    console.log('[useOBSConnection] Subscribing to SignalR events');

    // Subscribe to SignalR events
    signalRService.onConnectionStatusChanged(handleConnectionStatusChanged);
    signalRService.onSceneChanged(handleSceneChanged);
    signalRService.onStreamingStatusChanged(handleStreamingStatusChanged);

    // Cleanup: unsubscribe from events
    return () => {
      console.log('[useOBSConnection] Unsubscribing from SignalR events');
      signalRService.off('ConnectionStatusChanged', handleConnectionStatusChanged);
      signalRService.off('SceneChanged', handleSceneChanged);
      signalRService.off('StreamingStatusChanged', handleStreamingStatusChanged);
    };
  }, []);

  return {
    obsStatus,
    currentScene,
    isStreaming,
  };
};

