/**
 * OBS connection status information
 */
export interface OBSConnectionStatus {
  IsConnected: boolean;
  ServerUrl?: string;
  LastError?: string;
  LastConnectionAttempt?: string;
  ConnectedAt?: string;
}

/**
 * OBS scene information
 */
export interface Scene {
  Name: string;
  IsActive: boolean;
  Index: number;
}

/**
 * Response from getting scenes
 */
export interface ScenesResponse {
  CurrentScene: string;
  Scenes: Scene[];
}

/**
 * Request to switch scenes
 */
export interface SwitchSceneRequest {
  SceneName: string;
}

/**
 * Streaming status information
 */
export interface StreamingStatus {
  IsStreaming: boolean;
  IsRecording: boolean;
  StreamDuration: number;
}

/**
 * Scene item information
 */
export interface SceneItem {
  SceneItemId: number;
  SceneItemIndex: number;
  SourceName: string;
  SourceUuid: string;
  SourceType: string;
  SceneItemEnabled: boolean;
}

/**
 * Media input status information
 */
export interface MediaInputStatus {
  MediaState: string;
  MediaDuration: number | null;
  MediaCursor: number | null;
  RemainingTime: number | null;
}

export interface SceneMediaStatus {
  SceneName: string;
  MediaInputName: string | null;
  Status: MediaInputStatus | null;
}

/**
 * SignalR connection state
 */
export enum SignalRConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting',
  Disconnecting = 'Disconnecting',
}

