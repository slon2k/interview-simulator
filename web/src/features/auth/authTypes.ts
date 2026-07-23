export type CurrentUser = {
  isAuthenticated: boolean
  isInvited: boolean
  isAdmin: boolean
  userId: string | null
  identityProvider: string | null
  displayName: string | null
  githubLogin: string | null
  avatarUrl: string | null
}
