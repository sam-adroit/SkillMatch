import { useCallback, useEffect, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from './assets/vite.svg'
import heroImg from './assets/hero.png'
import './App.css'

type WeatherForecast = {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string
}

const apiUrl = import.meta.env.VITE_API_URL?.replace(/\/$/, '')

function App() {
  const [count, setCount] = useState(0)
  const [forecasts, setForecasts] = useState<WeatherForecast[]>([])
  const [weatherError, setWeatherError] = useState<string | null>(null)
  const [isLoadingWeather, setIsLoadingWeather] = useState(true)

  const loadWeather = useCallback(async () => {
    setIsLoadingWeather(true)
    setWeatherError(null)

    try {
      if (!apiUrl) {
        throw new Error('VITE_API_URL is not configured')
      }

      const response = await fetch(`${apiUrl}/WeatherForecast`)

      if (!response.ok) {
        throw new Error(`Weather request failed (${response.status})`)
      }

      const data: WeatherForecast[] = await response.json()
      setForecasts(data)
    } catch (error) {
      setWeatherError(
        error instanceof Error ? error.message : 'Unable to load the weather',
      )
    } finally {
      setIsLoadingWeather(false)
    }
  }, [])

  useEffect(() => {
    void loadWeather()
  }, [loadWeather])

  return (
    <>
      <section id="center">
        <div className="hero">
          <img src={heroImg} className="base" width="170" height="179" alt="" />
          <img src={reactLogo} className="framework" alt="React logo" />
          <img src={viteLogo} className="vite" alt="Vite logo" />
        </div>
        <div>
          <h1>Get started</h1>
          <p>
            Edit <code>src/App.tsx</code> and save to test <code>HMR</code>
          </p>
        </div>
        <button
          type="button"
          className="counter"
          onClick={() => setCount((count) => count + 1)}
        >
          Count is {count}
        </button>
      </section>

      <div className="ticks"></div>

      <section id="weather" aria-labelledby="weather-heading">
        <div className="weather-heading">
          <div>
            <h2 id="weather-heading">Weather API test</h2>
            <p>
              Calling <code>{apiUrl ?? 'VITE_API_URL'}/WeatherForecast</code>
            </p>
          </div>
          <button type="button" className="counter" onClick={loadWeather}>
            Refresh
          </button>
        </div>

        {isLoadingWeather && <p>Loading weather forecasts…</p>}

        {weatherError && (
          <p className="weather-error" role="alert">
            {weatherError}
          </p>
        )}

        {!isLoadingWeather && !weatherError && (
          <div className="weather-grid">
            {forecasts.map((forecast) => (
              <article className="weather-card" key={forecast.date}>
                <time dateTime={forecast.date}>{forecast.date}</time>
                <strong>{forecast.temperatureC}°C</strong>
                <span>
                  {forecast.temperatureF}°F · {forecast.summary}
                </span>
              </article>
            ))}
          </div>
        )}
      </section>

      <div className="ticks"></div>

      <section id="next-steps">
        <div id="docs">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#documentation-icon"></use>
          </svg>
          <h2>Documentation</h2>
          <p>Your questions, answered</p>
          <ul>
            <li>
              <a href="https://vite.dev/" target="_blank">
                <img className="logo" src={viteLogo} alt="" />
                Explore Vite
              </a>
            </li>
            <li>
              <a href="https://react.dev/" target="_blank">
                <img className="button-icon" src={reactLogo} alt="" />
                Learn more
              </a>
            </li>
          </ul>
        </div>
        <div id="social">
          <svg className="icon" role="presentation" aria-hidden="true">
            <use href="/icons.svg#social-icon"></use>
          </svg>
          <h2>Connect with us</h2>
          <p>Join the Vite community</p>
          <ul>
            <li>
              <a href="https://github.com/vitejs/vite" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#github-icon"></use>
                </svg>
                GitHub
              </a>
            </li>
            <li>
              <a href="https://chat.vite.dev/" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#discord-icon"></use>
                </svg>
                Discord
              </a>
            </li>
            <li>
              <a href="https://x.com/vite_js" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#x-icon"></use>
                </svg>
                X.com
              </a>
            </li>
            <li>
              <a href="https://bsky.app/profile/vite.dev" target="_blank">
                <svg
                  className="button-icon"
                  role="presentation"
                  aria-hidden="true"
                >
                  <use href="/icons.svg#bluesky-icon"></use>
                </svg>
                Bluesky
              </a>
            </li>
          </ul>
        </div>
      </section>

      <div className="ticks"></div>
      <section id="spacer"></section>
    </>
  )
}

export default App
