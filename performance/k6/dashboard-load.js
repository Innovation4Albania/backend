import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000/api').replace(/\/$/, '');
const ROLE = __ENV.ROLE || 'drejtor_agjencie';
const NAME = __ENV.NAME || 'Performance Test User';
const MINISTRY = __ENV.MINISTRY || null;
const USERNAME = __ENV.USERNAME || '';
const PASSWORD = __ENV.PASSWORD || '';
const ENABLE_WRITES = (__ENV.ENABLE_WRITES || 'false').toLowerCase() === 'true';
const READ_VUS = Number(__ENV.READ_VUS || 55);
const WRITE_VUS = Number(__ENV.WRITE_VUS || 5);
const STEADY_DURATION = __ENV.STEADY_DURATION || '2m';

const endpointErrors = new Rate('endpoint_errors');

const thresholds = {
  checks: ['rate>0.99'],
  endpoint_errors: ['rate<0.01'],
  http_req_failed: ['rate<0.01'],
  'http_req_duration{endpoint:dashboard_summary}': ['p(95)<750', 'p(99)<1500'],
  'http_req_duration{endpoint:projects}': ['p(95)<900', 'p(99)<1800'],
  'http_req_duration{endpoint:risk_deviations}': ['p(95)<900', 'p(99)<1800'],
};

const scenarios = {
  dashboard_reads: {
    executor: 'ramping-vus',
    exec: 'readDashboard',
    startVUs: 0,
    stages: [
      { duration: '30s', target: READ_VUS },
      { duration: STEADY_DURATION, target: READ_VUS },
      { duration: '15s', target: 0 },
    ],
    gracefulRampDown: '10s',
  },
};

if (ENABLE_WRITES) {
  thresholds['http_req_duration{endpoint:weekly_update}'] = ['p(95)<1200', 'p(99)<2500'];
  scenarios.weekly_updates = {
    executor: 'constant-vus',
    exec: 'submitWeeklyUpdate',
    startTime: '30s',
    vus: WRITE_VUS,
    duration: STEADY_DURATION,
    gracefulStop: '10s',
  };
}

export const options = {
  scenarios,
  thresholds,
};

function params(token, endpoint) {
  return {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
    tags: { endpoint },
  };
}

function expectStatus(response, status, endpoint) {
  const passed = check(response, {
    [`${endpoint} returns ${status}`]: (result) => result.status === status,
  });

  endpointErrors.add(!passed, { endpoint });
  return passed;
}

export function setup() {
  if (!USERNAME || !PASSWORD) {
    fail('USERNAME dhe PASSWORD duhet te vendosen per login-in e performance test.');
  }

  const loginResponse = http.post(
    `${BASE_URL}/auth/login`,
    JSON.stringify({
      role: ROLE,
      ministry: MINISTRY,
      name: NAME,
      username: USERNAME,
      password: PASSWORD,
    }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'login' } },
  );

  if (!expectStatus(loginResponse, 200, 'login')) {
    fail(`Login deshtoi me status ${loginResponse.status}: ${loginResponse.body}`);
  }

  const token = loginResponse.json('token');
  if (!token) {
    fail('Login nuk ktheu token.');
  }

  const projectsResponse = http.get(`${BASE_URL}/projects`, params(token, 'setup_projects'));
  if (!expectStatus(projectsResponse, 200, 'setup_projects')) {
    fail(`Leximi i projekteve deshtoi me status ${projectsResponse.status}.`);
  }

  const projects = projectsResponse.json();
  if (!Array.isArray(projects) || projects.length === 0) {
    fail('Performance test kerkon te pakten nje projekt te dukshem per perdoruesin.');
  }

  return { token, projectId: projects[0].id };
}

export function readDashboard(data) {
  const responses = http.batch([
    ['GET', `${BASE_URL}/dashboard/summary`, null, params(data.token, 'dashboard_summary')],
    ['GET', `${BASE_URL}/projects`, null, params(data.token, 'projects')],
    ['GET', `${BASE_URL}/risk-deviations`, null, params(data.token, 'risk_deviations')],
  ]);

  expectStatus(responses[0], 200, 'dashboard_summary');
  expectStatus(responses[1], 200, 'projects');
  expectStatus(responses[2], 200, 'risk_deviations');

  sleep(1 + Math.random() * 2);
}

export function submitWeeklyUpdate(data) {
  const progress = 20 + ((__VU + __ITER) % 75);
  const response = http.post(
    `${BASE_URL}/updates`,
    JSON.stringify({
      projectId: data.projectId,
      expertName: `Performance User ${__VU}`,
      progress,
      status: 'active',
      risk: 'medium',
      blockers: '',
      comments: 'Update i krijuar nga k6 performance test.',
      keyResults: [],
    }),
    params(data.token, 'weekly_update'),
  );

  expectStatus(response, 200, 'weekly_update');
  sleep(2 + Math.random() * 2);
}
