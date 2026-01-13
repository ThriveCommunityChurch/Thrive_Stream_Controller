import axios from 'axios';
import type { AxiosInstance } from 'axios';
import type { OBSConnectionStatus, ScenesResponse, SwitchSceneRequest, StreamingStatus, SceneItem, MediaInputStatus } from '@/types/obs';

/**
 * API service for making HTTP requests to the backend
 */
class ApiService {
  private axiosInstance: AxiosInstance;

  constructor() {
    this.axiosInstance = axios.create({
      baseURL: '/api',
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add response interceptor for error handling
    this.axiosInstance.interceptors.response.use(
      (response) => response,
      (error) => {
        console.error('API Error:', error);
        return Promise.reject(error);
      }
    );
  }

  /**
   * Generic HTTP methods
   */
  public async get<T>(url: string): Promise<T> {
    const response = await this.axiosInstance.get<T>(url);
    return response.data;
  }

  public async post<T>(url: string, data?: unknown): Promise<T> {
    const response = await this.axiosInstance.post<T>(url, data);
    return response.data;
  }

  /**
   * YouTube Auth API methods
   */
  public youtubeAuthAPI = {
    /**
     * Get YouTube OAuth status
     */
    getStatus: async (): Promise<{ IsConfigured: boolean; Platform: string; ConnectedAt?: string }> => {
      const response = await this.axiosInstance.get<{ IsConfigured: boolean; Platform: string; ConnectedAt?: string }>('/auth/youtube/status');
      return response.data;
    },

    /**
     * Get YouTube channel information
     */
    getChannelInfo: async (): Promise<{
      Id: string;
      Title: string;
      Description?: string;
      CustomUrl?: string;
      ThumbnailUrl?: string;
      SubscriberCount?: number;
      VideoCount?: number;
      ViewCount?: number;
      HiddenSubscriberCount: boolean;
      PublishedAt?: string;
    }> => {
      const response = await this.axiosInstance.get('/auth/youtube/channel');
      return response.data;
    },

    /**
     * Revoke YouTube OAuth tokens
     */
    revoke: async (): Promise<void> => {
      await this.axiosInstance.post('/auth/youtube/revoke');
    },
  };

  /**
   * Facebook Auth API methods
   */
  public facebookAuthAPI = {
    /**
     * Get Facebook OAuth status
     */
    getStatus: async (): Promise<{ IsConfigured: boolean; Platform: string; ConnectedAt?: string }> => {
      const response = await this.axiosInstance.get<{ IsConfigured: boolean; Platform: string; ConnectedAt?: string }>('/auth/facebook/status');
      return response.data;
    },

    /**
     * Revoke Facebook OAuth tokens
     */
    revoke: async (): Promise<void> => {
      await this.axiosInstance.post('/auth/facebook/revoke');
    },
  };

  /**
   * OBS API methods
   */
  public obsAPI = {
    /**
     * Connect to OBS WebSocket
     */
    connect: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/connect');
    },

    /**
     * Disconnect from OBS WebSocket
     */
    disconnect: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/disconnect');
    },

    /**
     * Get OBS connection status
     */
    getStatus: async (): Promise<OBSConnectionStatus> => {
      const response = await this.axiosInstance.get<OBSConnectionStatus>('/OBS/status');
      return response.data;
    },

    /**
     * Get list of OBS scenes
     */
    getScenes: async (): Promise<ScenesResponse> => {
      const response = await this.axiosInstance.get<ScenesResponse>('/OBS/scenes');
      return response.data;
    },

    /**
     * Switch to a different OBS scene
     */
    switchScene: async (sceneName: string): Promise<void> => {
      const request: SwitchSceneRequest = { SceneName: sceneName };
      await this.axiosInstance.post('/OBS/scenes/switch', request);
    },

    /**
     * Get streaming status
     */
    getStreamingStatus: async (): Promise<StreamingStatus> => {
      const response = await this.axiosInstance.get<StreamingStatus>('/OBS/streaming/status');
      return response.data;
    },

    /**
     * Start streaming
     */
    startStreaming: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/streaming/start');
    },

    /**
     * Stop streaming
     */
    stopStreaming: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/streaming/stop');
    },

    /**
     * Get scene items for a specific scene
     */
    getSceneItems: async (sceneName: string): Promise<SceneItem[]> => {
      const response = await this.axiosInstance.get<SceneItem[]>(`/OBS/scenes/${encodeURIComponent(sceneName)}/items`);
      return response.data;
    },

    /**
     * Get media input status for a specific input
     */
    getMediaInputStatus: async (inputName: string): Promise<MediaInputStatus> => {
      const response = await this.axiosInstance.get<MediaInputStatus>(`/OBS/media/${encodeURIComponent(inputName)}/status`);
      return response.data;
    },

    /**
     * Get virtual camera status
     */
    getVirtualCamStatus: async (): Promise<{ outputActive: boolean }> => {
      const response = await this.axiosInstance.get<{ outputActive: boolean }>('/OBS/virtualcam/status');
      return response.data;
    },

    /**
     * Start virtual camera
     */
    startVirtualCam: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/virtualcam/start');
    },

    /**
     * Stop virtual camera
     */
    stopVirtualCam: async (): Promise<void> => {
      await this.axiosInstance.post('/OBS/virtualcam/stop');
    },
  };

  /**
   * YouTube Live API methods
   */
  public youtubeLiveAPI = {
    /**
     * Create a new broadcast
     */
    createBroadcast: async (title: string, description?: string, scheduledStartTime?: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.post<YouTubeBroadcastInfo>('/youtube/live/broadcast', {
        Title: title,
        Description: description,
        ScheduledStartTime: scheduledStartTime,
      });
      return response.data;
    },

    /**
     * Get persistent stream info
     */
    getPersistentStream: async (): Promise<YouTubeStreamInfo> => {
      const response = await this.axiosInstance.get<YouTubeStreamInfo>('/youtube/live/stream');
      return response.data;
    },

    /**
     * Bind broadcast to stream
     */
    bindBroadcast: async (broadcastId: string): Promise<void> => {
      await this.axiosInstance.post(`/youtube/live/broadcast/${broadcastId}/bind`);
    },

    /**
     * Transition broadcast to testing
     */
    transitionToTesting: async (broadcastId: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.post<YouTubeBroadcastInfo>(`/youtube/live/broadcast/${broadcastId}/testing`);
      return response.data;
    },

    /**
     * Transition broadcast to live
     */
    transitionToLive: async (broadcastId: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.post<YouTubeBroadcastInfo>(`/youtube/live/broadcast/${broadcastId}/live`);
      return response.data;
    },

    /**
     * End broadcast
     */
    endBroadcast: async (broadcastId: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.post<YouTubeBroadcastInfo>(`/youtube/live/broadcast/${broadcastId}/end`);
      return response.data;
    },

    /**
     * Get broadcast status
     */
    getBroadcastStatus: async (broadcastId: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.get<YouTubeBroadcastInfo>(`/youtube/live/broadcast/${broadcastId}`);
      return response.data;
    },

    /**
     * Get active broadcast
     */
    getActiveBroadcast: async (): Promise<YouTubeBroadcastInfo | null> => {
      try {
        const response = await this.axiosInstance.get<YouTubeBroadcastInfo>('/youtube/live/broadcast/active');
        return response.data;
      } catch {
        return null;
      }
    },

    /**
     * Get broadcast defaults
     */
    getDefaults: async (): Promise<YouTubeBroadcastDefaults> => {
      const response = await this.axiosInstance.get<YouTubeBroadcastDefaults>('/youtube/live/defaults');
      return response.data;
    },

    /**
     * Update broadcast metadata
     */
    updateBroadcast: async (broadcastId: string, title?: string, description?: string): Promise<YouTubeBroadcastInfo> => {
      const response = await this.axiosInstance.patch<YouTubeBroadcastInfo>(`/youtube/live/broadcast/${broadcastId}`, {
        Title: title,
        Description: description,
      });
      return response.data;
    },
  };
}

// YouTube Live types
export interface YouTubeBroadcastInfo {
  Id: string;
  Title: string;
  Description?: string;
  ScheduledStartTime?: string;
  ActualStartTime?: string;
  ActualEndTime?: string;
  LifecycleStatus: string;
  PrivacyStatus: string;
  BoundStreamId?: string;
  WatchUrl?: string;
  EmbedHtml?: string;
  ThumbnailUrl?: string;
  IsLive: boolean;
  IsComplete: boolean;
  ErrorMessage?: string;
}

export interface YouTubeStreamInfo {
  Id: string;
  Title: string;
  StreamKey: string;
  RtmpUrl: string;
  IngestionAddress: string;
  HealthStatus?: string;
  IsActive: boolean;
}

export interface YouTubeBroadcastDefaults {
  TitleTemplate?: string;
  Description?: string;
  PrivacyStatus: string;
  ThumbnailPath?: string;
  SourceBroadcastId?: string;
}

// Export a singleton instance
export const apiService = new ApiService();

