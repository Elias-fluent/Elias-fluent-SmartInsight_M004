import { render, screen } from '@testing-library/react';
import { Button } from '../Button';
import { describe, it, expect } from 'vitest';

describe('Button Component', () => {
  it('renders the button with default props', () => {
    render(<Button>Click me</Button>);
    const button = screen.getByRole('button', { name: /click me/i });
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('h-10');
  });

  it('applies size variants correctly', () => {
    render(<Button size="sm">Small Button</Button>);
    const button = screen.getByRole('button', { name: /small button/i });
    expect(button).toHaveClass('h-9');
  });

  it('applies variant styles correctly', () => {
    render(<Button variant="outline">Outline Button</Button>);
    const button = screen.getByRole('button', { name: /outline button/i });
    expect(button).toHaveClass('border-input');
  });

  it('forwards additional props to the button element', () => {
    render(<Button data-testid="test-button">Test Button</Button>);
    const button = screen.getByTestId('test-button');
    expect(button).toBeInTheDocument();
  });
}); 