import { CheckCircleIcon, XCircleIcon, ArrowPathIcon } from '@heroicons/react/24/solid';
import type { OBSConnectionStatus } from '../types/obs';
import { SignalRConnectionState } from '@/types/obs';

interface ConnectionStatusProps {
  signalRState: SignalRConnectionState;
  obsStatus: OBSConnectionStatus;
}

/**
 * Component to display SignalR and OBS connection status
 */
export const ConnectionStatus: React.FC<ConnectionStatusProps> = ({
  signalRState,
  obsStatus,
}) => {
  const getSignalRIcon = () => {
    switch (signalRState) {
      case SignalRConnectionState.Connected:
        return <CheckCircleIcon className="w-5 h-5 text-green-500" />;
      case SignalRConnectionState.Connecting:
      case SignalRConnectionState.Reconnecting:
        return <ArrowPathIcon className="w-5 h-5 text-yellow-500 animate-spin" />;
      case SignalRConnectionState.Disconnected:
      case SignalRConnectionState.Disconnecting:
      default:
        return <XCircleIcon className="w-5 h-5 text-red-500" />;
    }
  };

  const getSignalRStatusText = () => {
    switch (signalRState) {
      case SignalRConnectionState.Connected:
        return 'Connected';
      case SignalRConnectionState.Connecting:
        return 'Connecting...';
      case SignalRConnectionState.Reconnecting:
        return 'Reconnecting...';
      case SignalRConnectionState.Disconnecting:
        return 'Disconnecting...';
      case SignalRConnectionState.Disconnected:
      default:
        return 'Disconnected';
    }
  };

  const getOBSIcon = () => {
    if (obsStatus.IsConnected) {
      return <CheckCircleIcon className="w-5 h-5 text-green-500" />;
    }
    return <XCircleIcon className="w-5 h-5 text-red-500" />;
  };

  const getOBSStatusColor = () => {
    return obsStatus.IsConnected ? 'text-green-700' : 'text-red-700';
  };

  const bothConnected = signalRState === SignalRConnectionState.Connected && obsStatus.IsConnected;

  return (
    <div className="flex flex-col items-end">
      {/* Status Icons Row */}
      <div className="flex items-center space-x-4">
        {/* Backend Connection - Icon Only */}
        <div className="flex items-center" title={`Backend: ${getSignalRStatusText()}`}>
          {getSignalRIcon()}
        </div>

        {/* OBS Connection - Icon + Label */}
        <div className="flex items-center space-x-2" title={obsStatus.IsConnected ? 'OBS Connected' : 'OBS Disconnected'}>
          {getOBSIcon()}
          <span className={`text-sm font-semibold ${getOBSStatusColor()}`}>
            OBS Studio
          </span>
        </div>
      </div>

      {/* Ready to Stream Message */}
      {bothConnected && obsStatus.ServerUrl && (
        <div className="mt-2 text-right">
          <p className="text-xs text-gray-600">
            Ready to stream via: <span className="font-medium">{obsStatus.ServerUrl}</span>
          </p>
        </div>
      )}
    </div>
  );
};

