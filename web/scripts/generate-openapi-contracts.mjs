import { spawn } from 'node:child_process'

const strictTls = process.argv.includes('--strict-tls')
const requireSchemaUrl = process.argv.includes('--require-schema-url')

if (requireSchemaUrl && !process.env.OPENAPI_SCHEMA_URL) {
  process.stderr.write('OPENAPI_SCHEMA_URL is required when --require-schema-url is set.\n')
  process.exit(1)
}

const schemaUrl = process.env.OPENAPI_SCHEMA_URL ?? 'https://localhost:5001/openapi/v1.json'
const outputPath = process.env.OPENAPI_OUTPUT_PATH ?? 'src/api/contracts/openapi.ts'

const env = normalizeEnv(process.env)
if (!strictTls && env.NODE_TLS_REJECT_UNAUTHORIZED === undefined && isHttpsLocalhost(schemaUrl)) {
  env.NODE_TLS_REJECT_UNAUTHORIZED = '0'
}

const command = `npm exec --yes --package openapi-typescript -- openapi-typescript ${schemaUrl} -o ${outputPath}`

const child = spawn(command, {
  env,
  stdio: 'inherit',
  shell: true,
})

child.on('exit', (code, signal) => {
  if (signal) {
    process.stderr.write(`OpenAPI generation terminated with signal ${signal}\n`)
    process.exit(1)
  }

  process.exit(code ?? 1)
})

child.on('error', (error) => {
  process.stderr.write(`Failed to start OpenAPI generator: ${error.message}\n`)
  process.exit(1)
})

function isHttpsLocalhost(url) {
  try {
    const parsed = new URL(url)
    return (
      parsed.protocol === 'https:' &&
      (parsed.hostname === 'localhost' || parsed.hostname === '127.0.0.1')
    )
  } catch {
    return false
  }
}

function normalizeEnv(sourceEnv) {
  const normalized = {}

  for (const [key, value] of Object.entries(sourceEnv)) {
    if (value === undefined || value === null) {
      continue
    }

    normalized[key] = String(value)
  }

  return normalized
}
