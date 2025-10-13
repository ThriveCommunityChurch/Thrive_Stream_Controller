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
}

// Export a singleton instance
export const apiService = new ApiService();

