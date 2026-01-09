import { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { ConnectionStatus } from './ConnectionStatus';
import { SceneSwitcher } from './SceneSwitcher';
import { ControlsMenu } from './ControlsMenu';
import { VideoPreview } from './VideoPreview';
import { AudioMeters } from './AudioMeters';
import { StreamingControls } from './StreamingControls';
import { useSignalR } from '@/hooks/useSignalR';
import { useOBSConnection } from '@/hooks/useOBSConnection';
import { useAudioMeterSettings } from '@/hooks/useAudioMeterSettings';
import { apiService } from '@/services/api.service';
import { SignalRConnectionState } from '@/types/obs';

/**
 * Main dashboard component integrating all features
 */
export const Dashboard: React.FC = () => {
  const { connectionState, isConnected: isSignalRConnected } = useSignalR();
  const { obsStatus, currentScene } = useOBSConnection();
  const { settings: audioMeterSettings, updateSettings: updateAudioMeterSettings } = useAudioMeterSettings();
  const [autoConnecting, setAutoConnecting] = useState<boolean>(false);
  const [autoConnectError, setAutoConnectError] = useState<string | null>(null);
  const [refreshKey, setRefreshKey] = useState<number>(0);
  const [isYouTubeConfigured, setIsYouTubeConfigured] = useState<boolean>(false);

  // Check YouTube configuration status
  useEffect(() => {
    const checkYouTubeStatus = async () => {
      try {
        const status = await apiService.youtubeAuthAPI.getStatus();
        setIsYouTubeConfigured(status.IsConfigured);
      } catch {
        setIsYouTubeConfigured(false);
      }
    };

    checkYouTubeStatus();
  }, []);

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
              audioMeterSettings={audioMeterSettings}
              onAudioMeterSettingsChange={updateAudioMeterSettings}
            />
            <div>
              <h1 className="text-4xl font-bold text-gray-800 mb-2">
                Thrive Stream Controller
              </h1>
              <p className="text-gray-600">
                Manage your livestreams with ease
              </p>
            </div>
          </div>
          <div className="flex items-center space-x-4">
            <Link
              to="/settings"
              className="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium"
            >
              ⚙️ Configure Accounts
            </Link>
            <ConnectionStatus
              signalRState={connectionState}
              obsStatus={obsStatus}
            />
          </div>
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

        {/* Video Preview and Audio Meters Row */}
        <div className="mb-6 grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Video Preview - Takes 2 columns */}
          <div className="lg:col-span-2">
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h2 className="text-2xl font-bold text-gray-800 mb-4">Program Out</h2>
              <VideoPreview isOBSConnected={obsStatus.IsConnected} />
            </div>
          </div>

          {/* Audio Meters - Takes 1 column */}
          <div className="lg:col-span-1">
            <AudioMeters
              isOBSConnected={obsStatus.IsConnected}
              settings={audioMeterSettings}
            />
          </div>
        </div>

        {/* Streaming Controls and Scene Switcher Row */}
        <div className="mb-6 grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Streaming Controls - Takes 1 column */}
          <div className="lg:col-span-1">
            <StreamingControls
              isOBSConnected={obsStatus.IsConnected}
              isYouTubeConfigured={isYouTubeConfigured}
            />
          </div>

          {/* Scene Switcher - Takes 2 columns */}
          <div className="lg:col-span-2">
            <SceneSwitcher
              currentScene={currentScene}
              isOBSConnected={obsStatus.IsConnected}
              refreshKey={refreshKey}
            />
          </div>
        </div>

        {/* Footer */}
        <div className="text-center text-gray-500 text-sm mt-8">
          <p>Thrive Community Church Stream Controller © {new Date().getFullYear()} Thrive Community Church</p>
        </div>
      </div>
    </div>
  );
};

