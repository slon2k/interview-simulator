import type { InterviewSummary } from '../../api/interviewApi'

// Pure presentation helpers for the interview list, kept separate from the page
// component so the page file only exports components (react-refresh constraint).

export function formatProgress(interview: InterviewSummary): string {
  const answered = toCount(interview.answeredCount)
  const total = toCount(interview.questionCount)

  if (total <= 0) {
    return '0/0'
  }

  return `${answered}/${total}`
}

// answeredCount / questionCount are typed `number | string` by the generated
// contract (the .NET OpenAPI generator emits Int32 that way), so the string branch
// is a real case, not dead defensive code.
export function toCount(value: number | string): number {
  if (typeof value === 'number') {
    return value
  }

  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) ? parsed : 0
}

export function statusColor(status: string): string {
  switch (status) {
    case 'active':
      return 'blue'
    case 'completed':
      return 'green'
    default:
      return 'gray'
  }
}

export function statusAction(status: string): string {
  switch (status) {
    case 'active':
      return 'Continue'
    case 'completed':
      return 'View'
    default:
      return 'Open'
  }
}
