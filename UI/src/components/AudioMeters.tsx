import { useEffect, useState, useCallback } from 'react';
import { signalRService } from '@/services/signalr.service';
import type { AudioMeterSettings } from '@/hooks/useAudioMeterSettings';

interface InputVolumeMeter {
  InputName: string;
  InputUuid: string;
  InputLevelsMul: number[];
  InputMuted: boolean;
}

interface InputVolumeMetersData {
  Inputs: InputVolumeMeter[];
}

interface AudioMetersProps {
  isOBSConnected: boolean;
  settings: AudioMeterSettings;
}

/**
 * AudioMeters component that displays real-time audio level meters for all OBS inputs
 */
export const AudioMeters: React.FC<AudioMetersProps> = ({ isOBSConnected, settings }) => {
  const [volumeMeters, setVolumeMeters] = useState<InputVolumeMetersData>({ Inputs: [] });

  /**
   * Convert linear multiplier to decibels
   */
  const mulToDb = useCallback((mul: number): number => {
    if (mul <= 0) return -60; // Minimum dB
    const db = 20 * Math.log10(mul);
    return Math.max(-60, Math.min(0, db)); // Clamp between -60 and 0
  }, []);

  /**
   * Get color for the meter based on dB level
   */
  const getMeterColor = useCallback((db: number): string => {
    if (db > -6) return 'bg-red-500'; // Red for > -6dB (danger zone)
    if (db > -12) return 'bg-yellow-500'; // Yellow for -12dB to -6dB (warning)
    return 'bg-green-500'; // Green for < -12dB (safe)
  }, []);

  /**
   * Calculate meter fill percentage (0-100)
   */
  const getMeterPercentage = useCallback((db: number): number => {
    // Map -60dB to 0dB -> 0% to 100%
    return ((db + 60) / 60) * 100;
  }, []);

  /**
   * Subscribe to volume meters updates
   */
  useEffect(() => {
    if (!isOBSConnected) {
      setVolumeMeters({ Inputs: [] });
      return;
    }

    let logCounter = 0;
    const handleVolumeMetersChanged = (data: InputVolumeMetersData) => {
      // Log every 10000th update to help with debugging (events fire every 50ms = 20/sec)
      logCounter++;
      if (logCounter % 10000 === 0) {
        console.log('[AudioMeters] Received volume meters:', {
          inputCount: data.Inputs.length,
          inputs: data.Inputs.map(i => ({
            name: i.InputName,
            uuid: i.InputUuid,
            channelCount: i.InputLevelsMul.length,
            levels: i.InputLevelsMul
          }))
        });
      }
      setVolumeMeters(data);
    };

    console.log('[AudioMeters] Subscribing to VolumeMetersChanged event');
    signalRService.on('VolumeMetersChanged', handleVolumeMetersChanged);

    return () => {
      console.log('[AudioMeters] Unsubscribing from VolumeMetersChanged event');
      signalRService.off('VolumeMetersChanged', handleVolumeMetersChanged);
    };
  }, [isOBSConnected]);

  if (!isOBSConnected) {
    return (
      <div className="bg-gray-800 rounded-lg p-4">
        <h3 className="text-lg font-semibold text-gray-400 mb-2">Audio Meters</h3>
        <p className="text-sm text-gray-500">Connect to OBS to view audio levels</p>
      </div>
    );
  }

  if (volumeMeters.Inputs.length === 0) {
    return (
      <div className="bg-gray-800 rounded-lg p-4">
        <h3 className="text-lg font-semibold text-white mb-2">Audio Mixer</h3>
        <p className="text-sm text-gray-400 mb-2">No active audio sources detected</p>
        <p className="text-xs text-gray-500">
          Make sure you have audio sources enabled in OBS:
        </p>
        <ul className="text-xs text-gray-500 mt-2 space-y-1 list-disc list-inside">
          <li>Desktop Audio</li>
          <li>Mic/Aux</li>
          <li>Media sources with audio</li>
        </ul>
      </div>
    );
  }

  return (
    <div className="bg-gray-800 rounded-lg p-4">
      <h3 className="text-lg font-semibold text-white mb-4">
        Audio Mixer
        <span className="text-xs text-gray-400 ml-2 font-normal">
          ({volumeMeters.Inputs.length} source{volumeMeters.Inputs.length !== 1 ? 's' : ''})
        </span>
      </h3>
      <div className="space-y-4">
        {volumeMeters.Inputs.map((input) => {
          return (
            <div key={input.InputUuid || input.InputName} className="space-y-2">
              {/* Input Name */}
              <div className="flex justify-between items-center">
                <span className="text-sm font-medium text-white truncate max-w-[200px]" title={input.InputName}>
                  {input.InputName}
                </span>
              </div>

              {/* Channel Meters */}
              <div className="space-y-1.5">
                {input.InputLevelsMul.map((level, channelIdx) => {
                  const db = mulToDb(level);
                  const percentage = getMeterPercentage(db);
                  const color = getMeterColor(db);
                  const channelLabel = input.InputLevelsMul.length === 1
                    ? 'Mono'
                    : (channelIdx === 0 ? 'L' : 'R');

                  return (
                    <div key={channelIdx} className="space-y-0.5">
                      {/* Channel label and dB value */}
                      <div className="flex justify-between items-center">
                        <span className="text-xs text-gray-400 font-mono w-8">
                          {channelLabel}
                        </span>
                        {settings.showDbValues && (
                          <span className="text-xs text-gray-400 font-mono">
                            {db.toFixed(1)} dB
                          </span>
                        )}
                      </div>

                      {/* Meter Bar */}
                      <div className="relative h-4 bg-gray-700 rounded-full overflow-hidden">
                        {/* Background gradient marks */}
                        <div className="absolute inset-0 flex">
                          <div className="flex-1 border-r border-gray-600"></div>
                          <div className="flex-1 border-r border-gray-600"></div>
                          <div className="flex-1 border-r border-gray-600"></div>
                          <div className="flex-1"></div>
                        </div>

                        {/* Meter fill */}
                        <div
                          className={`absolute left-0 top-0 h-full transition-all duration-75 ${color}`}
                          style={{ width: `${percentage}%` }}
                        >
                          {/* Shine effect */}
                          <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white to-transparent opacity-20"></div>
                        </div>

                        {/* Peak indicator line at -6dB (90% mark) */}
                        <div className="absolute left-[90%] top-0 h-full w-0.5 bg-red-400 opacity-50"></div>

                        {/* dB Scale markers */}
                        {settings.showDbScale && (
                          <div className="absolute inset-0 flex items-center justify-between px-1 pointer-events-none">
                            {/* -60, -50, -40, -30, -20, -10, 0 */}
                            {[-50, -40, -30, -20, -10, -5].map((dbValue) => {
                              const position = ((dbValue + 60) / 60) * 100;
                              return (
                                <div
                                  key={dbValue}
                                  className="absolute text-[8px] font-mono text-gray-300 opacity-70"
                                  style={{ left: `${position}%`, transform: 'translateX(-50%)' }}
                                >
                                  {dbValue}
                                </div>
                              );
                            })}
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>

      {/* Legend */}
      <div className="mt-4 pt-4 border-t border-gray-700">
        <div className="flex items-center gap-4 text-xs text-gray-400">
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 bg-green-500 rounded"></div>
            <span>Safe</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 bg-yellow-500 rounded"></div>
            <span>Warning</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 bg-red-500 rounded"></div>
            <span>Danger</span>
          </div>
        </div>
      </div>
    </div>
  );
};

