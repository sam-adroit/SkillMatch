const apiUrl = import.meta.env.VITE_API_URL?.replace(/\/$/, '')

type ProblemResponse = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  public readonly status?: number

  constructor(
    message: string,
    status?: number,
  ) {
    super(message)
    this.status = status
  }
}

export async function apiRequest<T>(
  path: string,
  init: RequestInit = {},
  token?: string,
): Promise<T> {
  if (!apiUrl) {
    throw new ApiError('The SkillMatch API URL is not configured.')
  }

  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')

  if (init.body) {
    headers.set('Content-Type', 'application/json')
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiUrl}${path}`, { ...init, headers })

  if (!response.ok) {
    let problem: ProblemResponse | undefined

    try {
      problem = (await response.json()) as ProblemResponse
    } catch {
      problem = undefined
    }

    const validationMessage = problem?.errors
      ? Object.values(problem.errors).flat().join(' ')
      : undefined
    const message =
      validationMessage ||
      problem?.detail ||
      problem?.title ||
      `Request failed (${response.status}).`

    throw new ApiError(message, response.status)
  }

  return (await response.json()) as T
}
