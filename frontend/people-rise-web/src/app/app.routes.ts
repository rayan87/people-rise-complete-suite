import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },

  // Job library
  { path: 'jobs', pathMatch: 'full', loadComponent: () => import('./features/jobs/jobs').then(m => m.Jobs) },
  { path: 'jobs/:id', loadComponent: () => import('./features/jobs/job-detail').then(m => m.JobDetail) },

  // Salary builder
  { path: 'salary', loadComponent: () => import('./features/salary/salary').then(m => m.Salary) },

  // Evaluations
  { path: 'evaluation', pathMatch: 'full', loadComponent: () => import('./features/evaluation/evaluation-hub').then(m => m.EvaluationHub) },
  { path: 'evaluation/new', loadComponent: () => import('./features/evaluation/new-evaluation').then(m => m.NewEvaluation) },
  { path: 'methodology/versions/:id', loadComponent: () => import('./features/methodology/version-editor').then(m => m.VersionEditor) },
  { path: 'evaluation/:id', loadComponent: () => import('./features/evaluation/evaluation-detail').then(m => m.EvaluationDetail) },

  // Methodologies (separate from evaluations)
  { path: 'methodology', pathMatch: 'full', loadComponent: () => import('./features/methodology/methodology-list').then(m => m.MethodologyList) },
  { path: 'methodology/:id', loadComponent: () => import('./features/methodology/methodology-detail').then(m => m.MethodologyDetail) },

  // Grading structure (level × family grid)
  { path: 'grading', loadComponent: () => import('./features/grading/grading').then(m => m.Grading) },

  { path: 'settings', loadComponent: () => import('./features/settings/settings').then(m => m.Settings) },
  { path: 'settings/structure', loadComponent: () => import('./features/settings/settings-structure').then(m => m.SettingsStructure) },
  { path: '**', redirectTo: 'dashboard' },
];
