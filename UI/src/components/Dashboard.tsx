import { useEffect, useState, useCallback } from 'react';
import { ConnectionStatus } from './ConnectionStatus';
import { SceneSwitcher } from './SceneSwitcher';
import { ControlsMenu } from './ControlsMenu';
import { useSignalR } from '@/hooks/useSignalR';
import { useOBSConnection } from '@/hooks/useOBSConnection';
import { apiService } from '@/services/api.service';
import { SignalRConnectionState } from '@/types/obs';

/**
 * Main dashboard component integrating all features
 */
export const Dashboard: React.FC = () => {
  const { connectionState, isConnected: isSignalRConnected } = useSignalR();
  const { obsStatus, currentScene } = useOBSConnection();
  const [autoConnecting, setAutoConnecting] = useState<boolean>(false);
  const [autoConnectError, setAutoConnectError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState<number>(0);

  // Auto-connect to OBS when SignalR is connected
  // Add a small delay to ensure SignalR connection is fully established
  useEffect(() => {
    // Don't auto-connect if already connected or currently connecting
    if (obsStatus.IsConnected || autoConnecting) {
      return;
    }

    // Only auto-connect when SignalR is fully connected
    if (!isSignalRConnected || connectionState !== SignalRConnectionState.Connected) {
      return;
    }

    // Add a small delay to ensure everything is ready
    const timeoutId = setTimeout(async () => {
      setAutoConnecting(true);
      setAutoConnectError(null);
      try {
        console.log('Auto-connecting to OBS...');
        await apiService.obsAPI.connect();
        console.log('Auto-connect to OBS initiated');
      } catch (error) {
        console.error('Failed to auto-connect to OBS:', error);
        setAutoConnectError(
          error instanceof Error ? error.message : 'Failed to connect to OBS'
        );
      } finally {
        setAutoConnecting(false);
      }
    }, 500); // 500ms delay to ensure SignalR is fully ready

    return () => clearTimeout(timeoutId);
  }, [isSignalRConnected, obsStatus.IsConnected, connectionState]);

  const handleManualConnect = async () => {
    setAutoConnecting(true);
    setAutoConnectError(null);
    try {
      await apiService.obsAPI.connect();
    } catch (error) {
      console.error('Failed to connect to OBS:', error);
      setAutoConnectError(
        error instanceof Error ? error.message : 'Failed to connect to OBS'
      );
    } finally {
      setAutoConnecting(false);
    }
  };

  const handleDisconnect = async () => {
    try {
      await apiService.obsAPI.disconnect();
    } catch (error) {
      console.error('Failed to disconnect from OBS:', error);
    }
  };

  const handleRefreshScenes = useCallback(() => {
    setRefreshKey(prev => prev + 1);
  }, []);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8 flex items-start justify-between">
          <div className="flex items-start space-x-4">
            <ControlsMenu
              isSignalRConnected={isSignalRConnected}
              isOBSConnected={obsStatus.IsConnected}
              autoConnecting={autoConnecting}
              onConnect={handleManualConnect}
              onDisconnect={handleDisconnect}
              onRefreshScenes={handleRefreshScenes}
            />
            <div>
              <h1 className="text-4xl font-bold text-gray-800 mb-2">
                Thrive Stream Controller
              </h1>
              <p className="text-gray-600">
                Manage your OBS Studio livestreams with ease
              </p>
            </div>
          </div>
          <ConnectionStatus
            signalRState={connectionState}
            obsStatus={obsStatus}
          />
        </div>

        {/* Auto-connect error message */}
        {autoConnectError && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-sm text-red-600">
              <span className="font-semibold">Connection Error:</span> {autoConnectError}
            </p>
            <button
              onClick={handleManualConnect}
              disabled={autoConnecting}
              className="mt-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
            >
              {autoConnecting ? 'Connecting...' : 'Retry Connection'}
            </button>
          </div>
        )}



        {/* Scene Switcher */}
        <div className="mb-6">
          <SceneSwitcher
            currentScene={currentScene}
            isOBSConnected={obsStatus.IsConnected}
            refreshKey={refreshKey}
          />
        </div>

        {/* Footer */}
        <div className="text-center text-gray-500 text-sm mt-8">
          <p>Thrive Community Church Stream Controller © {new Date().getFullYear()} Thrive Community Church</p>
        </div>
      </div>
    </div>
  );
};

