import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AccessibilityModal } from '../AccessibilityModal';
import { axeTest, printAccessibilityViolations } from '../../../test-utils/accessibility';

// Mock the useTheme, useAnnounce and StoreContext hooks
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

// Mock AccessibilitySettings
vi.mock('../AccessibilitySettings', () => ({
  default: () => <div data-testid="accessibility-settings">Mocked Settings</div>,
}));

describe('AccessibilityModal', () => {
  const mockOnClose = vi.fn();

  beforeEach(() => {
    mockOnClose.mockClear();
  });

  it('should not render when closed', () => {
    render(<AccessibilityModal isOpen={false} onClose={mockOnClose} />);
    
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('should render when open', () => {
    render(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByTestId('accessibility-settings')).toBeInTheDocument();
    expect(screen.getByText('Mocked Settings')).toBeInTheDocument();
  });
  
  it('should close when clicking the close button', async () => {
    const user = userEvent.setup();
    
    render(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    const closeButton = screen.getByLabelText(/close accessibility settings/i);
    await user.click(closeButton);
    
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });
  
  it('should close when clicking outside the modal', async () => {
    const user = userEvent.setup();
    
    render(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    // Find the backdrop (the first div with role="dialog")
    const backdrop = screen.getByRole('dialog').firstChild;
    
    // Click the backdrop
    await user.click(backdrop as HTMLElement);
    
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });
  
  it('should close when pressing Escape key', async () => {
    const user = userEvent.setup();
    
    render(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    await user.keyboard('{Escape}');
    
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });
  
  it('should have no accessibility violations', async () => {
    const result = await axeTest(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    // @ts-ignore - Handling potential type issues with axe results
    const violations = result?.results?.violations || [];
    
    // Log violations for debugging if test fails
    if (violations.length > 0) {
      printAccessibilityViolations(violations);
    }
    
    // Expect no violations
    expect(violations.length).toBe(0);
  });
  
  it('should trap focus within the modal when open', async () => {
    const user = userEvent.setup();
    
    render(<AccessibilityModal isOpen={true} onClose={mockOnClose} />);
    
    // Try tabbing - focus should stay within the modal
    await user.tab(); // Focus first element (close button)
    expect(screen.getByLabelText(/close accessibility settings/i)).toHaveFocus();
    
    // Tab to settings content
    await user.tab();
    
    // Tab again (should cycle back to first focusable element)
    await user.tab();
    expect(screen.getByLabelText(/close accessibility settings/i)).toHaveFocus();
  });
}); 