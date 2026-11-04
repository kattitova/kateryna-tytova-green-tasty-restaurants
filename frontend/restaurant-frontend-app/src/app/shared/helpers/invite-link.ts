export function buildInviteLink(token: string): string {
  return `${window.location.origin}/join/${token}`;
}
