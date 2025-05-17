// Types for NodeJS namespace
declare namespace NodeJS {
  interface Timeout {}
  interface Process {
    env: {
      NODE_ENV: 'development' | 'production' | 'test';
      [key: string]: string | undefined;
    };
  }
} 