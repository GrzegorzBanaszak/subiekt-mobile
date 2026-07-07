export type AppIconName =
  | 'box'
  | 'cart'
  | 'clipboard'
  | 'pallet'
  | 'settings'
  | 'user'
  | 'search'
  | 'chevronLeft'
  | 'chevronRight'
  | 'image'
  | 'warning'
  | 'language'
  | 'lock'
  | 'login'
  | 'logout'
  | 'error'
  | 'add'
  | 'edit'
  | 'key'
  | 'block'
  | 'check'
  | 'arrowBack'
  | 'close'
  | 'organization'
  | 'personAdd'
  | 'personCheck'
  | 'refresh'
  | 'history'
  | 'info'

const symbols: Record<AppIconName, string> = {
  box: 'inventory_2',
  cart: 'shopping_cart',
  clipboard: 'inventory',
  pallet: 'pallet',
  settings: 'settings',
  user: 'person',
  search: 'search',
  chevronLeft: 'chevron_left',
  chevronRight: 'chevron_right',
  image: 'image',
  warning: 'warning',
  language: 'language',
  lock: 'lock',
  login: 'login',
  logout: 'logout',
  error: 'error',
  add: 'add',
  edit: 'edit',
  key: 'key',
  block: 'block',
  check: 'check_circle',
  arrowBack: 'arrow_back',
  close: 'close',
  organization: 'corporate_fare',
  personAdd: 'person_add',
  personCheck: 'person_check',
  refresh: 'refresh',
  history: 'history',
  info: 'info',
}

export function AppIcon({ name, className = 'size-6' }: { name: AppIconName; className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`material-symbols-outlined inline-flex shrink-0 items-center justify-center leading-none ${className}`}
    >
      {symbols[name]}
    </span>
  )
}
