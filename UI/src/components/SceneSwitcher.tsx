import { useState, useEffect, useCallback } from 'react';
import { VideoCameraIcon, ArrowPathIcon, ClockIcon } from '@heroicons/react/24/solid';
import { apiService } from '@/services/api.service';
import { useMediaStatus } from '@/hooks/useMediaStatus';
import type { Scene } from '@/types/obs';

interface SceneSwitcherProps {
  currentScene: string;
  isOBSConnected: boolean;
  refreshKey?: number;
}

/**
 * Component to display and switch between OBS scenes
 */
export const SceneSwitcher: React.FC<SceneSwitcherProps> = ({
  currentScene,
  isOBSConnected,
  refreshKey,
}) => {
  const [scenes, setScenes] = useState<Scene[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [switching, setSwitching] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Use the media status hook to get real-time updates via SignalR
  const { sceneMediaStatus } = useMediaStatus();

  const fetchScenes = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await apiService.obsAPI.getScenes();
      setScenes(response.Scenes || []);
    } catch (err) {
      console.error('Error fetching scenes:', err);
      setError('Failed to fetch scenes');
    } finally {
      setLoading(false);
    }
  }, []);

  // Auto-fetch scenes when OBS connects, clear when disconnects
  useEffect(() => {
    if (isOBSConnected) {
      fetchScenes();
    } else {
      setScenes([]);
    }
  }, [isOBSConnected, fetchScenes]);

  // Refresh scenes when refreshKey changes
  useEffect(() => {
    if (refreshKey && refreshKey > 0 && isOBSConnected) {
      fetchScenes();
    }
  }, [refreshKey, isOBSConnected, fetchScenes]);

  // Media status is now handled by the useMediaStatus hook via SignalR
  // No polling needed!

  // Update active scene when currentScene prop changes
  useEffect(() => {
    if (currentScene && scenes.length > 0) {
      // Update the IsActive flag for all scenes based on currentScene
      setScenes(prevScenes =>
        prevScenes.map(scene => ({
          ...scene,
          IsActive: scene.Name === currentScene
        }))
      );
    }
  }, [currentScene, scenes.length]);

  const handleSwitchScene = async (sceneName: string) => {
    if (sceneName === currentScene) {
      return; // Already on this scene
    }

    setSwitching(sceneName);
    setError(null);
    try {
      await apiService.obsAPI.switchScene(sceneName);
      // The scene change will be reflected via SignalR event
    } catch (err) {
      console.error('Error switching scene:', err);
      setError(`Failed to switch to ${sceneName}`);
    } finally {
      setSwitching(null);
    }
  };

  // Format milliseconds to HH:MM:SS or MM:SS
  const formatTime = (milliseconds: number): string => {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    if (hours > 0) {
      return `-${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    }
    if (minutes > 0) {
      return `-${minutes}:${seconds.toString().padStart(2, '0')}`;
    }
    if (seconds > 0) {
      return `-${seconds} seconds`;
    }
    return `ended`;
  };

  if (!isOBSConnected) {
    return (
      <div className="bg-white rounded-lg shadow-md p-6">
        <h2 className="text-xl font-bold mb-4 text-gray-800">Scene Switcher</h2>
        <div className="text-center py-8">
          <VideoCameraIcon className="w-16 h-16 text-gray-300 mx-auto mb-4" />
          <p className="text-gray-500">OBS is not connected</p>
          <p className="text-sm text-gray-400 mt-2">
            Connect to OBS to view and switch scenes
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg shadow-md p-6">
      <div className="mb-4">
        <h2 className="text-xl font-bold text-gray-800">Scene Switcher</h2>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{error}</p>
        </div>
      )}

      {loading ? (
        <div className="text-center py-8">
          <ArrowPathIcon className="w-8 h-8 text-blue-500 animate-spin mx-auto mb-2" />
          <p className="text-gray-500">Loading scenes...</p>
        </div>
      ) : !scenes || scenes.length === 0 ? (
        <div className="text-center py-8">
          <VideoCameraIcon className="w-16 h-16 text-gray-300 mx-auto mb-4" />
          <p className="text-gray-500 font-medium">No scenes loaded</p>
          <p className="text-sm text-gray-400 mt-2">
            Use the menu to refresh scenes from OBS
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {scenes.map((scene) => {
            const isCurrentScene = scene.Name === currentScene || scene.IsActive;
            const isSwitching = switching === scene.Name;
            const mediaStatus = sceneMediaStatus[scene.Name]?.Status;
            const hasMediaTime = mediaStatus && mediaStatus.RemainingTime !== null && mediaStatus.RemainingTime > 0;

            return (
              <button
                key={scene.Name}
                onClick={() => handleSwitchScene(scene.Name)}
                disabled={isCurrentScene || isSwitching}
                className={`
                  p-4 rounded-lg border-2 transition-all text-left
                  ${
                    isCurrentScene
                      ? 'border-blue-500 bg-blue-50 cursor-default'
                      : 'border-gray-200 bg-white hover:border-blue-300 hover:bg-blue-50'
                  }
                  ${isSwitching ? 'opacity-50 cursor-wait' : ''}
                  disabled:cursor-not-allowed
                `}
              >
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <VideoCameraIcon
                      className={`w-6 h-6 ${
                        isCurrentScene ? 'text-blue-600' : 'text-gray-400'
                      }`}
                    />
                    <div>
                      <p
                        className={`font-semibold ${
                          isCurrentScene ? 'text-blue-800' : 'text-gray-800'
                        }`}
                      >
                        {scene.Name}
                      </p>
                      {isCurrentScene && (
                        <p className="text-xs text-blue-600 mt-1">Active</p>
                      )}
                      {hasMediaTime && (
                        <div className="flex items-center space-x-1 mt-1">
                          <ClockIcon className="w-3 h-3 text-gray-500" />
                          <p className="text-xs text-gray-600">
                            {formatTime(mediaStatus.RemainingTime!)}
                          </p>
                        </div>
                      )}
                    </div>
                  </div>
                  {isSwitching && (
                    <ArrowPathIcon className="w-5 h-5 text-blue-500 animate-spin" />
                  )}
                </div>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
};

