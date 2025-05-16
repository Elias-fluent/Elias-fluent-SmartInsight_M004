# Accessibility Testing Guide

This guide provides instructions for performing both automated and manual accessibility testing for the SmartInsight UI.

## Automated Testing with axe-core

We use [axe-core](https://github.com/dequelabs/axe-core) to perform automated accessibility testing in our test suite.

### Running Accessibility Tests

To run all tests including accessibility tests:

```bash
npm test
```

To run tests in watch mode during development:

```bash
npm run test:watch
```

### Writing Accessibility Tests

For new components, follow this pattern:

1. Import the testing utilities:

```tsx
import { axeTest, printAccessibilityViolations } from '../../../test-utils/accessibility';
```

2. Add an accessibility test to your test suite:

```tsx
it('should have no accessibility violations', async () => {
  const result = await axeTest(<YourComponent />);
  
  // Handle potential type issues with axe results
  const violations = result?.results?.violations || [];
  
  // Log violations for debugging if test fails
  if (violations.length > 0) {
    printAccessibilityViolations(violations);
  }
  
  // Expect no violations
  expect(violations.length).toBe(0);
});
```

## Manual Accessibility Testing

While automated tests catch many issues, manual testing is essential for a complete accessibility evaluation.

### Keyboard Navigation Testing

Test the following keyboard interactions:

1. **Tab Order**: Press Tab to move through interactive elements. Verify focus moves in a logical order.
2. **Focus Visibility**: Ensure focus indicators are visible for all interactive elements.
3. **Keyboard Operability**: Test all interactions using only the keyboard:
   - Enter/Space to activate buttons and links
   - Arrow keys for navigation where appropriate
   - Escape to close dialogs and modals

### Screen Reader Testing

Test with at least one screen reader:

#### NVDA (Windows)
1. Download and install [NVDA](https://www.nvaccess.org/download/)
2. Use these commands:
   - Insert+Space: Start/stop reading
   - Insert+Down Arrow: Read from current position
   - Tab: Move between elements
   - NVDA+F7: List all elements

#### VoiceOver (Mac)
1. Enable VoiceOver: Cmd+F5
2. Use these commands:
   - VO+Right Arrow: Move to next element
   - VO+Space: Activate element
   - VO+U: Use rotor to navigate by headings, links, etc.

### Testing Checklist

For each component, verify:

#### Structure and Semantics
- [ ] Proper heading structure (h1-h6)
- [ ] Landmarks used correctly (main, nav, etc.)
- [ ] Lists marked up correctly
- [ ] Tables have proper headers and captions

#### Interactive Elements
- [ ] All controls have accessible names
- [ ] Custom controls have appropriate ARIA attributes
- [ ] Error messages are associated with their fields
- [ ] Form labels are properly connected to inputs

#### Visual Design
- [ ] Color is not the only means of conveying information
- [ ] Text has sufficient contrast with background
- [ ] Content is readable when zoomed to 200%
- [ ] Layout works with different text sizes

#### Multimedia
- [ ] Images have meaningful alt text
- [ ] Videos have captions and transcripts
- [ ] No content flashes more than 3 times per second

## Accessibility Features Testing

Our application has specific accessibility features that need testing:

### High Contrast Mode
1. Enable high contrast mode in accessibility settings
2. Verify all UI elements remain visible and distinguishable
3. Test both light and dark variations

### Color Blind Modes
1. Test each color blind mode (deuteranopia, protanopia, tritanopia)
2. Verify information conveyed by color remains perceivable
3. Check charts and data visualizations are still readable

### Text Scaling
1. Test each text size option
2. Verify layout accommodates larger text
3. Ensure no content gets cut off or overlaps

## Reporting Issues

When reporting accessibility issues:

1. Specify which WCAG criterion it violates (e.g., 1.1.1 Non-text Content)
2. Describe how to reproduce the issue
3. Note assistive technologies used (browser, screen reader, etc.)
4. Suggest a possible solution if known

## Resources

- [WCAG 2.1 Guidelines](https://www.w3.org/TR/WCAG21/)
- [axe-core Rules](https://github.com/dequelabs/axe-core/blob/master/doc/rule-descriptions.md)
- [WAI-ARIA Practices](https://www.w3.org/WAI/ARIA/apg/) 