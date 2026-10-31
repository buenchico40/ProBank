import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';
import 'dotenv/config';

const prototipo = 'file://' + path.resolve(import.meta.dirname, '../../prototype/ProBank_DevExtreme.html');

export default defineConfig({
  testDir: './specs',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  retries: process.env.CI ? 1 : 0,
  reporter: [['html', { outputFolder: 'reports/html', open: 'never' }], ['list'], ['junit', { outputFile: 'reports/junit.xml' }]],
  use: { trace: 'retain-on-failure', screenshot: 'only-on-failure', video: 'retain-on-failure', locale: 'es-EC', timezoneId: 'America/Guayaquil' },
  projects: [
    // Corre hoy contra la maqueta navegable (no necesita servidor ni Entra ID)
    { name: 'prototipo', use: { ...devices['Desktop Chrome'], viewport: { width: 1360, height: 900 }, baseURL: prototipo }, metadata: { prototipo: true } },
    // Contra el ambiente desplegado (inicia sesión con Entra ID en global-setup)
    { name: 'setup', testMatch: /global\.setup\.ts/, testDir: '.' },
    { name: 'escritorio', dependencies: ['setup'], use: { ...devices['Desktop Edge'], channel: 'msedge', baseURL: process.env.PROBANK_BASE_URL } },
    { name: 'telefono', dependencies: ['setup'], use: { ...devices['Pixel 7'], baseURL: process.env.PROBANK_BASE_URL } },
  ],
});
