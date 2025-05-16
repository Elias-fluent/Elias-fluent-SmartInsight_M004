import { run as runAxe } from 'axe-core';
import { expect } from 'vitest';
import type { ReactElement } from 'react';
import { render } from '@testing-library/react';

/**
 * Test a React component for accessibility violations
 * 
 * @param ui The React component to test
 * @param options Optional configuration options for axe
 * @returns The render result and axe results
 * 
 * @example
 * ```tsx
 * it('should have no accessibility violations', async () => {
 *   const { results } = await axeTest(<MyComponent />);
 *   expect(results.violations).toEqual([]);
 * });
 * ```
 */
export async function axeTest(
  ui: ReactElement,
  options = {}
) {
  const container = render(ui);
  
  // Create a configuration for axe
  const axeConfig = {
    ...options,
    runOnly: {
      type: 'tag',
      values: ['wcag2a', 'wcag2aa', 'wcag21aa', 'best-practice'],
    },
  };
  
  // Run axe
  // @ts-ignore - Type issues with axe-core
  const axeResults = await runAxe(container.container, axeConfig);
  
  return {
    container,
    results: axeResults,
  };
}

/**
 * Common configuration options for axe-core
 */
export const axeDefaultConfig = {
  // Common rules to check by default
  rules: {
    // Enable all WCAG AA rules
    'color-contrast': { enabled: true },
    'link-name': { enabled: true },
    'aria-roles': { enabled: true },
    'button-name': { enabled: true },
    'image-alt': { enabled: true },
    'label': { enabled: true },
    'landmark-one-main': { enabled: true },
    'region': { enabled: true },
  },
};

/**
 * Print detailed accessibility violations to the console
 * Useful for debugging accessibility issues
 * 
 * @param violations The violations from the axe results
 */
export function printAccessibilityViolations(violations: any[]) {
  if (violations.length === 0) {
    console.log('No accessibility violations detected');
    return;
  }

  console.group(`${violations.length} accessibility violation${violations.length === 1 ? '' : 's'} detected`);
  
  violations.forEach(violation => {
    console.group(`Impact: ${violation.impact} - ${violation.help}`);
    console.log(`Rule: ${violation.id}`);
    console.log(`Description: ${violation.description}`);
    console.log(`Help URL: ${violation.helpUrl}`);
    console.log(`Elements:`);
    
    violation.nodes.forEach((node: any) => {
      console.log(`- ${node.html}`);
      console.log(`  Target: ${node.target}`);
    });
    
    console.groupEnd();
  });
  
  console.groupEnd();
} 