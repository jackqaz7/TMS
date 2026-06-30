const coreApiBaseUrl =
  import.meta.env.VITE_CORE_API_BASE_URL ?? 'https://localhost:7218'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'DELETE'

type RequestOptions = {
  method?: HttpMethod
  token?: string
  body?: unknown
}

// This wrapper keeps API calls consistent while the backend contracts evolve.
export async function coreApiRequest<TResponse>(
  path: string,
  options: RequestOptions = {},
): Promise<TResponse> {
  const response = await fetch(`${coreApiBaseUrl}${path}`, {
    method: options.method ?? 'GET',
    headers: {
      'Content-Type': 'application/json',
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
    },
    body: options.body ? JSON.stringify(options.body) : undefined,
  })

  if (!response.ok) {
    throw new Error(`CoreAPI request failed with status ${response.status}`)
  }

  return response.json() as Promise<TResponse>
}
