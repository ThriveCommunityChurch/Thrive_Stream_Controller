import { useEffect, useState } from 'react';
import { signalRService } from '@/services/signalr.service';
import type { SceneMediaStatus } from '@/types/obs';

/**
 * Hook for listening to media status updates via SignalR
 */
export const useMediaStatus = () => {
  const [sceneMediaStatus, setSceneMediaStatus] = useState<Record<string, SceneMediaStatus>>({});

  useEffect(() => {
    // Handler for media status changes
    const handleMediaStatusChanged = (status: SceneMediaStatus) => {  
      setSceneMediaStatus(prev => ({
        ...prev,
        [status.SceneName]: status
      }));
    };

    console.log('[useMediaStatus] Subscribing to MediaStatusChanged event');

    // Subscribe to SignalR event
    signalRService.on('MediaStatusChanged', handleMediaStatusChanged);

    // Cleanup: unsubscribe from event
    return () => {
      console.log('[useMediaStatus] Unsubscribing from MediaStatusChanged event');
      signalRService.off('MediaStatusChanged', handleMediaStatusChanged);
    };
  }, []);

  return {
    sceneMediaStatus,
  };
};

