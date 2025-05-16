import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AccessibilitySettings from '../AccessibilitySettings';
import { axeTest, printAccessibilityViolations } from '../../../test-utils/accessibility';

// Mock the useTheme and useAnnounce hooks
vi.mock('../../../hooks/useTheme', () => ({
  useTheme: () => ({
    isDark: false,
    contrastMode: 'normal',
    colorBlindMode: 'normal',
    textSize: 'normal',
    setContrastMode: vi.fn(),
    setColorBlindMode: vi.fn(),
    setTextSize: vi.fn(),
    toggleContrastMode: vi.fn(),
  }),
}));

vi.mock('../../../hooks/useAnnounce', () => ({
  useAnnounce: () => ({
    announce: vi.fn(),
    announceAssertive: vi.fn(),
    announcePolite: vi.fn(),
  }),
}));

describe('AccessibilitySettings', () => {
  it('should render with all required controls', () => {
    render(<AccessibilitySettings />);
    
    // Check for headings
    expect(screen.getByText('Accessibility Settings')).toBeInTheDocument();
    
    // Check for contrast mode control
    expect(screen.getByLabelText(/contrast mode/i)).toBeInTheDocument();
    expect(screen.getByText('Normal Contrast')).toBeInTheDocument();
    expect(screen.getByText('High Contrast')).toBeInTheDocument();
    
    // Check for color mode control
    expect(screen.getByLabelText(/color mode/i)).toBeInTheDocument();
    expect(screen.getByText('Standard Colors')).toBeInTheDocument();
    expect(screen.getByText(/deuteranopia/i)).toBeInTheDocument();
    
    // Check for text size control
    expect(screen.getByLabelText(/text size/i)).toBeInTheDocument();
    expect(screen.getByText('Normal')).toBeInTheDocument();
    expect(screen.getByText('Large')).toBeInTheDocument();
    expect(screen.getByText('Extra Large')).toBeInTheDocument();
  });
  
  it('should have no accessibility violations', async () => {
    const result = await axeTest(<AccessibilitySettings />);
    
    // @ts-ignore - Handling potential type issues with axe results
    const violations = result?.results?.violations || [];
    
    // Log violations for debugging if test fails
    if (violations.length > 0) {
      printAccessibilityViolations(violations);
    }
    
    // Expect no violations
    expect(violations.length).toBe(0);
  });

  it('should provide explanatory text for each option', () => {
    render(<AccessibilitySettings />);
    
    // Check for help text that explains each option
    expect(screen.getByText(/high contrast mode increases visual distinction/i)).toBeInTheDocument();
    expect(screen.getByText(/optimizes colors for different types/i)).toBeInTheDocument();
    expect(screen.getByText(/adjusts the size of text/i)).toBeInTheDocument();
  });

  it('should have functioning controls with keyboard accessibility', async () => {
    const user = userEvent.setup();
    render(<AccessibilitySettings />);
    
    // Check that all controls are keyboard accessible
    const contrastSelect = screen.getByLabelText(/contrast mode/i);
    await user.tab(); // Focus on first element
    
    // Expect the contrast mode select to receive focus
    expect(contrastSelect).toHaveFocus();
    
    // Test keyboard-driven select option change (will trigger the mocked function)
    await user.keyboard('{ArrowDown}');
    await user.keyboard('{Enter}');
    
    // Tab to next control
    await user.tab();
    expect(screen.getByLabelText(/color mode/i)).toHaveFocus();
    
    // Tab to next control
    await user.tab();
    expect(screen.getByLabelText(/text size/i)).toHaveFocus();
  });
}); 