# Pruebas de punta a punta (Playwright + TypeScript)

```bash
npm install
npx playwright install
npm run test:prototipo          # contra prototype/ProBank_DevExtreme.html (sin servidor)
npx playwright test --project=escritorio --project=telefono   # contra el ambiente (requiere .env)
npx playwright test --grep @PRY-04                              # solo un requisito
npm run report
```

- Cada prueba lleva la etiqueta de su requisito (`spec/trazabilidad.md`).
- `pages/dx.ts` concentra cómo se manejan los controles DevExtreme.
- El reporte HTML, las trazas y las capturas son evidencia de pruebas.
