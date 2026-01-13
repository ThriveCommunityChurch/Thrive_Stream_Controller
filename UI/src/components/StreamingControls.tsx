import { useState, useEffect, useCallback } from 'react';
import { apiService, type YouTubeBroadcastInfo } from '@/services/api.service';

interface StreamingControlsProps {
  isOBSConnected: boolean;
  isYouTubeConfigured: boolean;
  facebookLiveProducerUrl?: string;
}

type StreamingPhase = 
  | 'idle'
  | 'creating_broadcast'
  | 'binding_stream'
  | 'starting_obs'
  | 'transitioning_live'
  | 'live'
  | 'ending'
  | 'error';

/**
 * Streaming controls component for volunteer-friendly stream management
 */
export const StreamingControls: React.FC<StreamingControlsProps> = ({
  isOBSConnected,
  isYouTubeConfigured,
  facebookLiveProducerUrl = 'https://www.facebook.com/live/producer',
}) => {
  const [phase, setPhase] = useState<StreamingPhase>('idle');
  const [currentBroadcast, setCurrentBroadcast] = useState<YouTubeBroadcastInfo | null>(null);
  const [broadcastTitle, setBroadcastTitle] = useState('');
  const [broadcastDescription, setBroadcastDescription] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // Load defaults on mount
  useEffect(() => {
    const loadDefaults = async () => {
      if (!isYouTubeConfigured) return;
      
      try {
        const defaults = await apiService.youtubeLiveAPI.getDefaults();
        if (defaults.TitleTemplate) {
          // Replace date placeholder if present
          const today = new Date().toLocaleDateString('en-US', { 
            month: 'long', 
            day: 'numeric', 
            year: 'numeric' 
          });
          setBroadcastTitle(defaults.TitleTemplate.replace('{date}', today));
        }
        if (defaults.Description) {
          setBroadcastDescription(defaults.Description);
        }
      } catch (err) {
        console.error('Failed to load defaults:', err);
      }
    };

    loadDefaults();
  }, [isYouTubeConfigured]);

  // Check for active broadcast on mount
  useEffect(() => {
    const checkActiveBroadcast = async () => {
      if (!isYouTubeConfigured) return;
      
      try {
        const active = await apiService.youtubeLiveAPI.getActiveBroadcast();
        if (active) {
          setCurrentBroadcast(active);
          setPhase('live');
        }
      } catch {
        // No active broadcast
      }
    };

    checkActiveBroadcast();
  }, [isYouTubeConfigured]);

  const handleStartStream = useCallback(async () => {
    if (!isOBSConnected || !isYouTubeConfigured) return;
    
    setIsLoading(true);
    setError(null);
    
    try {
      // Phase 1: Create broadcast
      setPhase('creating_broadcast');
      const broadcast = await apiService.youtubeLiveAPI.createBroadcast(
        broadcastTitle || 'Sunday Worship Service',
        broadcastDescription
      );
      setCurrentBroadcast(broadcast);

      // Phase 2: Bind to persistent stream
      setPhase('binding_stream');
      await apiService.youtubeLiveAPI.bindBroadcast(broadcast.Id);

      // Phase 3: Start OBS streaming
      setPhase('starting_obs');
      await apiService.obsAPI.startStreaming();
      
      // Wait a moment for stream to connect
      await new Promise(resolve => setTimeout(resolve, 5000));

      // Phase 4: Transition to testing first (YouTube requirement)
      await apiService.youtubeLiveAPI.transitionToTesting(broadcast.Id);
      
      // Wait for testing status
      await new Promise(resolve => setTimeout(resolve, 3000));

      // Phase 5: Transition to live
      setPhase('transitioning_live');
      const liveBroadcast = await apiService.youtubeLiveAPI.transitionToLive(broadcast.Id);
      setCurrentBroadcast(liveBroadcast);
      
      setPhase('live');
    } catch (err) {
      console.error('Failed to start stream:', err);
      setError(err instanceof Error ? err.message : 'Failed to start stream');
      setPhase('error');
    } finally {
      setIsLoading(false);
    }
  }, [isOBSConnected, isYouTubeConfigured, broadcastTitle, broadcastDescription]);

  const handleEndStream = useCallback(async () => {
    if (!currentBroadcast) return;
    
    setIsLoading(true);
    setError(null);
    
    try {
      setPhase('ending');
      
      // Stop OBS streaming first
      await apiService.obsAPI.stopStreaming();
      
      // End YouTube broadcast
      await apiService.youtubeLiveAPI.endBroadcast(currentBroadcast.Id);
      
      setCurrentBroadcast(null);
      setPhase('idle');
    } catch (err) {
      console.error('Failed to end stream:', err);
      setError(err instanceof Error ? err.message : 'Failed to end stream');
      setPhase('error');
    } finally {
      setIsLoading(false);
    }
  }, [currentBroadcast]);

  const handleOpenFacebook = useCallback(() => {
    window.open(facebookLiveProducerUrl, '_blank');
  }, [facebookLiveProducerUrl]);

  const getPhaseMessage = (): string => {
    switch (phase) {
      case 'creating_broadcast': return 'Creating YouTube broadcast...';
      case 'binding_stream': return 'Binding to stream...';
      case 'starting_obs': return 'Starting OBS stream...';
      case 'transitioning_live': return 'Going live on YouTube...';
      case 'live': return 'LIVE';
      case 'ending': return 'Ending stream...';
      case 'error': return 'Error';
      default: return 'Ready';
    }
  };

  const isStreaming = phase === 'live';
  const canStart = phase === 'idle' && isOBSConnected && isYouTubeConfigured && !isLoading;
  const canEnd = isStreaming && !isLoading;

  return (
    <div className="bg-white rounded-lg shadow-lg p-6">
      <h2 className="text-2xl font-bold text-gray-800 mb-4">Stream Controls</h2>

      {/* Status Indicator */}
      <div className={`mb-4 p-3 rounded-lg ${
        isStreaming ? 'bg-red-100 border border-red-300' :
        phase === 'error' ? 'bg-yellow-100 border border-yellow-300' :
        'bg-gray-100 border border-gray-200'
      }`}>
        <div className="flex items-center justify-between">
          <span className="font-medium">
            Status: <span className={isStreaming ? 'text-red-600 font-bold' : 'text-gray-600'}>
              {getPhaseMessage()}
            </span>
          </span>
          {isStreaming && (
            <span className="flex items-center">
              <span className="w-3 h-3 bg-red-500 rounded-full animate-pulse mr-2" />
              <span className="text-red-600 font-bold">● REC</span>
            </span>
          )}
        </div>
        {currentBroadcast?.WatchUrl && (
          <a
            href={currentBroadcast.WatchUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-blue-600 hover:underline"
          >
            {currentBroadcast.WatchUrl}
          </a>
        )}
      </div>

      {/* Error Display */}
      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-sm text-red-600">{error}</p>
          <button
            onClick={() => { setError(null); setPhase('idle'); }}
            className="mt-2 text-sm text-red-600 hover:underline"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Broadcast Settings (only show when not streaming) */}
      {!isStreaming && phase === 'idle' && (
        <div className="mb-4 space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Stream Title
            </label>
            <input
              type="text"
              value={broadcastTitle}
              onChange={(e) => setBroadcastTitle(e.target.value)}
              placeholder="Sunday Worship Service"
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Description (optional)
            </label>
            <textarea
              value={broadcastDescription}
              onChange={(e) => setBroadcastDescription(e.target.value)}
              placeholder="Join us for worship..."
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
          </div>
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex flex-col gap-3">
        {!isStreaming ? (
          <button
            onClick={handleStartStream}
            disabled={!canStart}
            className={`w-full py-4 px-6 rounded-lg font-bold text-lg transition-all ${
              canStart
                ? 'bg-green-600 hover:bg-green-700 text-white shadow-lg hover:shadow-xl'
                : 'bg-gray-300 text-gray-500 cursor-not-allowed'
            }`}
          >
            {isLoading ? (
              <span className="flex items-center justify-center">
                <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                {getPhaseMessage()}
              </span>
            ) : (
              '▶ Start Stream (OBS + YouTube)'
            )}
          </button>
        ) : (
          <button
            onClick={handleEndStream}
            disabled={!canEnd}
            className={`w-full py-4 px-6 rounded-lg font-bold text-lg transition-all ${
              canEnd
                ? 'bg-red-600 hover:bg-red-700 text-white shadow-lg hover:shadow-xl'
                : 'bg-gray-300 text-gray-500 cursor-not-allowed'
            }`}
          >
            {isLoading ? 'Ending Stream...' : '⏹ End Stream'}
          </button>
        )}

        <button
          onClick={handleOpenFacebook}
          className="w-full py-3 px-6 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-all shadow hover:shadow-lg"
        >
          📺 Open Facebook to Go Live
        </button>
      </div>

      {/* Help Text */}
      {!isOBSConnected && (
        <p className="mt-4 text-sm text-yellow-600">
          ⚠️ OBS is not connected. Please ensure OBS is running.
        </p>
      )}
      {!isYouTubeConfigured && (
        <p className="mt-4 text-sm text-yellow-600">
          ⚠️ YouTube is not configured. Go to Settings to connect your account.
        </p>
      )}
    </div>
  );
};
