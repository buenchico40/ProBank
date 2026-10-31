// Carga de la API de consultas y del pool de conexiones: k6 run test/load/consultas.k6.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [{ duration: '1m', target: 20 }, { duration: '3m', target: 60 }, { duration: '1m', target: 0 }],
  thresholds: { http_req_failed: ['rate<0.01'], http_req_duration: ['p(95)<1500'] },
};

const BASE = __ENV.PROBANK_API;
const TOKEN = __ENV.PROBANK_TOKEN;

export default function () {
  const r = http.get(`${BASE}/api/portafolio/tareas?mias=true`, { headers: { Authorization: `Bearer ${TOKEN}` } });
  check(r, { '200': (x) => x.status === 200 });
  sleep(1);
}
