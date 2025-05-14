# SmartInsight UI

This is the front-end application for the SmartInsight platform, built with React, TypeScript, and Tailwind CSS.

## Features

- React 18 with TypeScript
- Tailwind CSS for styling
- shadcn/ui component library integration
- ESLint and Prettier for code quality
- Responsive design

## Prerequisites

- Node.js (v14.0.0 or later)
- npm (v6.0.0 or later)

## Getting Started

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start the development server:
   ```bash
   npm start
   ```

3. Build for production:
   ```bash
   npm run build
   ```

## Project Structure

- `/src/components` - Reusable UI components
  - `/src/components/ui` - shadcn/ui components
- `/src/pages` - Application pages
- `/src/hooks` - Custom React hooks
- `/src/utils` - Utility functions
- `/src/context` - React context providers
- `/src/services` - API services
- `/src/assets` - Static assets

## Styling

This project uses Tailwind CSS for styling. The main configuration is in `tailwind.config.js`.

## Accessibility

The UI components are designed to be WCAG 2.1 AA compliant. All components should:

- Have appropriate ARIA attributes
- Support keyboard navigation
- Have sufficient color contrast
- Include proper focus states

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Edge (latest)
- Safari (latest) 