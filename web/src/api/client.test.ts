import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, download, setUnauthorizedHandler, tokenStore } from './client';

function mockFetch(status: number, body?: unknown) {
  const fetchMock = vi.fn().mockResolvedValue(
    new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    }),
  );
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('api', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    setUnauthorizedHandler(null);
  });

  it('sends the token and json body', async () => {
    tokenStore.set('abc');
    const fetchMock = mockFetch(200, { ok: true });

    const result = await api<{ ok: boolean }>('/things', { method: 'POST', body: { name: 'x' } });

    expect(result).toEqual({ ok: true });
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(url).toBe('/api/things');
    expect(init.headers.Authorization).toBe('Bearer abc');
    expect(init.headers['Content-Type']).toBe('application/json');
    expect(init.body).toBe('{"name":"x"}');
  });

  it('returns undefined for 204', async () => {
    mockFetch(204);
    await expect(api('/things/1', { method: 'DELETE' })).resolves.toBeUndefined();
  });

  it('uses the problem detail as the message', async () => {
    mockFetch(409, { title: 'Conflict', detail: 'Already exists.' });

    const error = await api('/x').catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(409);
    expect((error as ApiError).message).toBe('Already exists.');
  });

  it('exposes validation errors per field', async () => {
    mockFetch(400, {
      title: 'One or more validation errors occurred.',
      errors: { Email: ['Bad email'] },
    });

    const error = (await api('/x').catch((e: unknown) => e)) as ApiError;

    expect(error.message).toBe('Bad email');
    expect(error.fieldError('email')).toBe('Bad email');
  });

  it('calls the unauthorized handler when a signed-in request gets 401', async () => {
    tokenStore.set('expired');
    mockFetch(401);
    const handler = vi.fn();
    setUnauthorizedHandler(handler);

    await api('/x').catch(() => undefined);

    expect(handler).toHaveBeenCalledOnce();
  });

  it('turns network failures into a friendly error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));

    const error = (await api('/x').catch((e: unknown) => e)) as ApiError;

    expect(error.status).toBe(0);
    expect(error.message).toMatch(/could not reach the server/i);
  });

  it('downloads files with the token and the server file name', async () => {
    tokenStore.set('abc');
    const fetchMock = vi.fn().mockResolvedValue(
      new Response('a,b\r\n', {
        status: 200,
        headers: { 'Content-Disposition': 'attachment; filename="plan-2026-04-29.csv"' },
      }),
    );
    vi.stubGlobal('fetch', fetchMock);
    URL.createObjectURL = vi.fn(() => 'blob:x');
    URL.revokeObjectURL = vi.fn();
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

    await download('/boards/b1/export.csv', 'board.csv');

    expect(fetchMock.mock.calls[0]![1].headers.Authorization).toBe('Bearer abc');
    const anchor = click.mock.instances[0] as unknown as HTMLAnchorElement;
    expect(anchor.download).toBe('plan-2026-04-29.csv');
    click.mockRestore();
  });
});
