import React from 'react';
import { useTheme } from '../../hooks/useTheme';
import { useAnnounce } from '../../hooks/useAnnounce';

interface AccessibilitySettingsProps {
  className?: string;
}

/**
 * Component for managing accessibility settings
 * Provides controls for adjusting contrast, color modes, and text size
 */
export function AccessibilitySettings({ className = '' }: AccessibilitySettingsProps) {
  const { 
    isDark, 
    contrastMode, 
    colorBlindMode, 
    textSize,
    setContrastMode, 
    setColorBlindMode, 
    setTextSize 
  } = useTheme();
  
  const { announce } = useAnnounce();
  
  // Handler for contrast mode change
  const handleContrastChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const newMode = event.target.value as 'normal' | 'high';
    setContrastMode(newMode);
    announce(`Contrast mode set to ${newMode}`);
  };
  
  // Handler for color blind mode change
  const handleColorBlindModeChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const newMode = event.target.value as 'normal' | 'deuteranopia' | 'protanopia' | 'tritanopia';
    setColorBlindMode(newMode);
    announce(`Color mode set to ${newMode}`);
  };
  
  // Handler for text size change
  const handleTextSizeChange = (event: React.ChangeEvent<HTMLSelectElement>) => {
    const newSize = event.target.value as 'normal' | 'large' | 'x-large';
    setTextSize(newSize);
    announce(`Text size set to ${newSize}`);
  };
  
  return (
    <div className={`p-4 rounded-lg border border-border ${className}`} aria-labelledby="accessibility-settings-heading">
      <h2 
        id="accessibility-settings-heading" 
        className="text-lg font-medium mb-4"
        tabIndex={-1}
      >
        Accessibility Settings
      </h2>
      
      <div className="space-y-6">
        {/* Contrast Mode Setting */}
        <div className="space-y-2">
          <label 
            htmlFor="contrast-mode" 
            className="block font-medium"
          >
            Contrast Mode
          </label>
          <select
            id="contrast-mode"
            value={contrastMode}
            onChange={handleContrastChange}
            className="w-full px-3 py-2 border border-input rounded-md bg-background"
            aria-describedby="contrast-mode-help"
          >
            <option value="normal">Normal Contrast</option>
            <option value="high">High Contrast</option>
          </select>
          <p id="contrast-mode-help" className="text-sm text-muted-foreground">
            High contrast mode increases visual distinction between elements
          </p>
        </div>
        
        {/* Color Blind Mode Setting */}
        <div className="space-y-2">
          <label 
            htmlFor="color-blind-mode" 
            className="block font-medium"
          >
            Color Mode
          </label>
          <select
            id="color-blind-mode"
            value={colorBlindMode}
            onChange={handleColorBlindModeChange}
            className="w-full px-3 py-2 border border-input rounded-md bg-background"
            aria-describedby="color-blind-mode-help"
          >
            <option value="normal">Standard Colors</option>
            <option value="deuteranopia">Deuteranopia (Red-Green)</option>
            <option value="protanopia">Protanopia (Red-Green)</option>
            <option value="tritanopia">Tritanopia (Blue-Yellow)</option>
          </select>
          <p id="color-blind-mode-help" className="text-sm text-muted-foreground">
            Optimizes colors for different types of color vision
          </p>
        </div>
        
        {/* Text Size Setting */}
        <div className="space-y-2">
          <label 
            htmlFor="text-size" 
            className="block font-medium"
          >
            Text Size
          </label>
          <select
            id="text-size"
            value={textSize}
            onChange={handleTextSizeChange}
            className="w-full px-3 py-2 border border-input rounded-md bg-background"
            aria-describedby="text-size-help"
          >
            <option value="normal">Normal</option>
            <option value="large">Large</option>
            <option value="x-large">Extra Large</option>
          </select>
          <p id="text-size-help" className="text-sm text-muted-foreground">
            Adjusts the size of text throughout the application
          </p>
        </div>
        
        {/* A sample of how colors appear with current settings */}
        <div className="space-y-2 mt-6">
          <h3 className="font-medium">Preview</h3>
          <div className="p-4 border border-border rounded-md bg-card">
            <div className="flex flex-wrap gap-3">
              <span className="px-3 py-1 rounded-md bg-primary text-primary-foreground">
                Primary
              </span>
              <span className="px-3 py-1 rounded-md bg-secondary text-secondary-foreground">
                Secondary
              </span>
              <span className="px-3 py-1 rounded-md bg-accent text-accent-foreground">
                Accent
              </span>
              <span className="px-3 py-1 rounded-md bg-destructive text-destructive-foreground">
                Destructive
              </span>
              <span className="px-3 py-1 rounded-md bg-muted text-muted-foreground">
                Muted
              </span>
            </div>
            <div className="mt-3">
              <p className="text-foreground">
                Sample text with <a href="#" className="text-primary underline">links</a> and 
                <strong> emphasized content</strong>.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AccessibilitySettings; 