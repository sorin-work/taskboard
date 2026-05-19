import { screen } from '@testing-library/react';
import { useParams } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { lastWorkspace } from '../lib/lastWorkspace';
import { mockApi, renderWithProviders } from '../test/utils';
import { HomePage } from './HomePage';

const workspaces = [
  { id: 'w1', name: 'Home', role: 'owner', memberCount: 1, createdAt: '' },
  { id: 'w2', name: 'Studio', role: 'member', memberCount: 4, createdAt: '' },
];
const routes = [{ path: '/w/:workspaceId', element: <Remembered /> }];

describe('HomePage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('goes to the workspace used last', async () => {
    lastWorkspace.set('w2');
    mockApi({ 'GET /api/workspaces': () => ({ body: workspaces }) });
    renderWithProviders(<HomePage />, { path: '/', extraRoutes: routes });

    expect(await screen.findByText('opened w2')).toBeInTheDocument();
  });

  it('falls back to the first workspace when the remembered one is gone', async () => {
    lastWorkspace.set('deleted');
    mockApi({ 'GET /api/workspaces': () => ({ body: workspaces }) });
    renderWithProviders(<HomePage />, { path: '/', extraRoutes: routes });

    expect(await screen.findByText('opened w1')).toBeInTheDocument();
  });

  it('offers to create a workspace when there are none', async () => {
    mockApi({ 'GET /api/workspaces': () => ({ body: [] }) });
    renderWithProviders(<HomePage />, { path: '/' });

    expect(await screen.findByRole('button', { name: 'Create workspace' })).toBeInTheDocument();
  });
});

function Remembered() {
  const { workspaceId } = useParams();
  return <p>opened {workspaceId}</p>;
}
