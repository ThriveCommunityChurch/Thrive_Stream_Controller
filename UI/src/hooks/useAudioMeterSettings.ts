import { useState, useEffect } from 'react';

export interface AudioMeterSettings {
  showDbValues: boolean;
  showDbScale: boolean;
}

const STORAGE_KEY = 'audioMeterSettings';

const DEFAULT_SETTINGS: AudioMeterSettings = {
  showDbValues: true,
  showDbScale: false,
};

/**
 * Custom hook to manage audio meter settings in localStorage
 */
export const useAudioMeterSettings = () => {
  const [settings, setSettings] = useState<AudioMeterSettings>(() => {
    // Load settings from localStorage on initial render
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        return { ...DEFAULT_SETTINGS, ...JSON.parse(stored) };
      }
    } catch (error) {
      console.error('Failed to load audio meter settings from localStorage:', error);
    }
    return DEFAULT_SETTINGS;
  });

  // Save settings to localStorage whenever they change
  useEffect(() => {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
    } catch (error) {
      console.error('Failed to save audio meter settings to localStorage:', error);
    }
  }, [settings]);

  const updateSettings = (updates: Partial<AudioMeterSettings>) => {
    setSettings((prev) => ({ ...prev, ...updates }));
  };

  return {
    settings,
    updateSettings,
  };
};

