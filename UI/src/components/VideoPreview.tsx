import { useEffect, useRef, useState, useCallback } from 'react';
import { apiService } from '@/services/api.service';

interface VideoPreviewProps {
  isOBSConnected: boolean;
}

/**
 * VideoPreview component that displays live video from OBS Virtual Camera
 * Uses WebRTC getUserMedia to access the virtual camera device
 */
export const VideoPreview: React.FC<VideoPreviewProps> = ({ isOBSConnected }) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isVirtualCamActive, setIsVirtualCamActive] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  /**
   * Start the OBS Virtual Camera
   */
  const startVirtualCamera = useCallback(async () => {
    try {
      console.log('Starting OBS Virtual Camera...');
      await apiService.obsAPI.startVirtualCam();
      setIsVirtualCamActive(true);
      setError(null);
      // Wait a moment for the virtual camera to initialize
      await new Promise(resolve => setTimeout(resolve, 1000));
    } catch (err) {
      console.error('Failed to start virtual camera:', err);
      setError('Failed to start OBS Virtual Camera');
    }
  }, []);

  /**
   * Access the OBS Virtual Camera using WebRTC
   */
  const startVideoStream = useCallback(async () => {
    if (!videoRef.current) return;

    setIsLoading(true);
    setError(null);

    try {
      // First, request camera permissions to enumerate devices properly
      // We'll request any camera first, then switch to OBS Virtual Camera
      let stream: MediaStream;

      try {
        // Try to get OBS Virtual Camera directly by requesting video with constraints
        stream = await navigator.mediaDevices.getUserMedia({
          video: {
            width: { ideal: 1920 },
            height: { ideal: 1080 },
            aspectRatio: { ideal: 16/9 },
          },
          audio: false,
        });

        // Now enumerate devices to find OBS Virtual Camera
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices.filter(device => device.kind === 'videoinput');

        // Look for OBS Virtual Camera
        const obsCamera = videoDevices.find(device =>
          device.label.toLowerCase().includes('obs') ||
          device.label.toLowerCase().includes('virtual')
        );

        if (obsCamera) {
          console.log('Found OBS Virtual Camera:', obsCamera.label);

          // Stop the current stream
          stream.getTracks().forEach(track => track.stop());

          // Request the specific OBS Virtual Camera
          stream = await navigator.mediaDevices.getUserMedia({
            video: {
              deviceId: { exact: obsCamera.deviceId },
              width: { ideal: 1920 },
              height: { ideal: 1080 },
              aspectRatio: { ideal: 16/9 },
            },
            audio: false,
          });
        } else {
          console.warn('OBS Virtual Camera not found in device list, using default camera');
        }
      } catch (err) {
        console.error('Error getting camera stream:', err);
        throw err;
      }

      streamRef.current = stream;
      videoRef.current.srcObject = stream;

      console.log('Video stream started successfully');
      setError(null);
    } catch (err) {
      console.error('Error accessing virtual camera:', err);

      if (err instanceof Error) {
        if (err.name === 'NotAllowedError') {
          setError('Camera permission denied. Please allow camera access and refresh the page.');
        } else if (err.name === 'NotFoundError') {
          setError('No camera found. Please start the OBS Virtual Camera.');
        } else if (err.name === 'NotReadableError') {
          setError('Camera is already in use by another application.');
        } else {
          setError(err.message);
        }
      } else {
        setError('Failed to access camera');
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  /**
   * Stop the video stream
   */
  const stopVideoStream = useCallback(() => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop());
      streamRef.current = null;
    }
    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
  }, []);

  /**
   * Toggle fullscreen mode
   */
  const toggleFullscreen = useCallback(async () => {
    if (!containerRef.current) return;

    try {
      if (!document.fullscreenElement) {
        await containerRef.current.requestFullscreen();
        setIsFullscreen(true);
      } else {
        await document.exitFullscreen();
        setIsFullscreen(false);
      }
    } catch (err) {
      console.error('Error toggling fullscreen:', err);
    }
  }, []);

  /**
   * Handle fullscreen change events
   */
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => {
      document.removeEventListener('fullscreenchange', handleFullscreenChange);
    };
  }, []);

  /**
   * Initialize video stream when OBS connects
   */
  useEffect(() => {
    if (!isOBSConnected) {
      stopVideoStream();
      setIsVirtualCamActive(false);
      return;
    }

    // Auto-start virtual camera and video stream when OBS connects
    const initializePreview = async () => {
      try {
        // Check if virtual camera is already active
        const status = await apiService.obsAPI.getVirtualCamStatus();
        
        if (!status.outputActive) {
          // Start virtual camera if not active
          await startVirtualCamera();
        } else {
          setIsVirtualCamActive(true);
        }

        // Start video stream
        await startVideoStream();
      } catch (err) {
        console.error('Failed to initialize video preview:', err);
      }
    };

    initializePreview();

    // Cleanup on unmount or when OBS disconnects
    return () => {
      stopVideoStream();
    };
  }, [isOBSConnected, startVirtualCamera, startVideoStream, stopVideoStream]);

  /**
   * Retry connection
   */
  const handleRetry = useCallback(async () => {
    setError(null);
    
    // Try to start virtual camera if not active
    if (!isVirtualCamActive) {
      await startVirtualCamera();
    }
    
    // Try to start video stream
    await startVideoStream();
  }, [isVirtualCamActive, startVirtualCamera, startVideoStream]);

  if (!isOBSConnected) {
    return (
      <div className="bg-gray-800 rounded-lg p-8 text-center">
        <div className="text-gray-400 mb-4">
          <svg className="w-16 h-16 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 10l4.553-2.276A1 1 0 0121 8.618v6.764a1 1 0 01-1.447.894L15 14M5 18h8a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z" />
          </svg>
          <p className="text-lg font-medium">OBS Not Connected</p>
          <p className="text-sm mt-2">Connect to OBS to view live program output</p>
        </div>
      </div>
    );
  }

  return (
    <div ref={containerRef} className={`relative bg-black rounded-lg overflow-hidden ${isFullscreen ? 'w-screen h-screen' : ''}`}>
      {/* Video Element */}
      <video
        ref={videoRef}
        autoPlay
        playsInline
        muted
        className="w-full h-full object-contain"
        style={{ maxHeight: isFullscreen ? '100vh' : '600px' }}
      />

      {/* Loading Overlay */}
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-75">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-white mx-auto mb-4"></div>
            <p className="text-white">Loading video preview...</p>
          </div>
        </div>
      )}

      {/* Error Overlay */}
      {error && (
        <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-75">
          <div className="text-center max-w-md px-4">
            <svg className="w-16 h-16 mx-auto mb-4 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <p className="text-white text-lg font-medium mb-2">Video Preview Error</p>
            <p className="text-gray-300 text-sm mb-4">{error}</p>
            <button
              onClick={handleRetry}
              className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              Retry
            </button>
          </div>
        </div>
      )}

      {/* Controls Overlay */}
      {!isLoading && !error && (
        <div className="absolute bottom-4 right-4 flex gap-2">
          <button
            onClick={toggleFullscreen}
            className="p-2 bg-black bg-opacity-50 hover:bg-opacity-75 text-white rounded-lg transition-colors"
            title={isFullscreen ? 'Exit Fullscreen' : 'Fullscreen'}
          >
            {isFullscreen ? (
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            ) : (
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
              </svg>
            )}
          </button>
        </div>
      )}

      {/* Status Indicator */}
      {!isLoading && !error && (
        <div className="absolute top-4 left-4">
          <div className="flex items-center gap-2 px-3 py-1 bg-black bg-opacity-50 rounded-full">
            <div className="w-2 h-2 bg-red-500 rounded-full animate-pulse"></div>
            <span className="text-white text-sm font-medium">LIVE</span>
          </div>
        </div>
      )}
    </div>
  );
};

