import { useState, useRef, useEffect } from 'react';
import { Bars3Icon, ArrowPathIcon } from '@heroicons/react/24/solid';

interface ControlsMenuProps {
  isSignalRConnected: boolean;
  isOBSConnected: boolean;
  autoConnecting: boolean;
  onConnect: () => void;
  onDisconnect: () => void;
  onRefreshScenes: () => void;
}

/**
 * Dropdown menu for OBS controls
 */
export const ControlsMenu: React.FC<ControlsMenuProps> = ({
  isSignalRConnected,
  isOBSConnected,
  autoConnecting,
  onConnect,
  onDisconnect,
  onRefreshScenes,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  const handleMenuItemClick = (action: () => void) => {
    action();
    setIsOpen(false);
  };

  return (
    <div className="relative" ref={menuRef}>
      {/* Menu Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="p-2 text-gray-700 hover:bg-gray-100 rounded-lg transition-colors"
        title="Controls Menu"
      >
        <Bars3Icon className="w-6 h-6" />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <div className="absolute left-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-gray-200 py-2 z-50">
          {/* Connect to OBS */}
          <button
            onClick={() => handleMenuItemClick(onConnect)}
            disabled={isOBSConnected || autoConnecting || !isSignalRConnected}
            className="w-full px-4 py-2 text-left text-sm hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span className="font-medium text-gray-900">
              {autoConnecting ? 'Connecting...' : 'Connect to OBS'}
            </span>
            {!isSignalRConnected && (
              <span className="block text-xs text-gray-500 mt-1">
                Waiting for backend...
              </span>
            )}
          </button>

          {/* Disconnect from OBS */}
          <button
            onClick={() => handleMenuItemClick(onDisconnect)}
            disabled={!isOBSConnected}
            className="w-full px-4 py-2 text-left text-sm hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span className="font-medium text-gray-900">Disconnect from OBS</span>
          </button>

          {/* Divider */}
          <div className="my-2 border-t border-gray-200"></div>

          {/* Refresh Scenes */}
          <button
            onClick={() => handleMenuItemClick(onRefreshScenes)}
            disabled={!isOBSConnected}
            className="w-full px-4 py-2 text-left text-sm hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center space-x-2"
          >
            <ArrowPathIcon className="w-4 h-4 text-gray-600" />
            <span className="font-medium text-gray-900">Refresh Scenes</span>
          </button>
        </div>
      )}
    </div>
  );
};

