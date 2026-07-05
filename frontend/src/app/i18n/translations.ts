export const polishTranslations = {
  'app.loading': 'Ładowanie aplikacji…',
  'home.eyebrow': 'Frontend gotowy',
  'home.title': 'Podgląd danych Subiekta GT',
  'home.description':
    'Podstawa aplikacji jest przygotowana pod listę i szczegóły towarów.',
  'language.label': 'Język',
  'language.polish': 'Polski',
  'language.spanish': 'Hiszpański',
  'login.title': 'Subiekt Mobile',
  'login.subtitle': 'Logowanie do systemu magazynowego',
  'login.username': 'Użytkownik',
  'login.usernamePlaceholder': 'Wprowadź nazwę użytkownika',
  'login.password': 'Hasło',
  'login.passwordPlaceholder': 'Wprowadź hasło',
  'login.submit': 'Zaloguj',
  'login.submitting': 'Logowanie…',
  'login.help': 'Potrzebujesz pomocy? Skontaktuj się z administratorem.',
  'login.error.invalidCredentials': 'Nieprawidłowy użytkownik lub hasło.',
  'login.error.tooManyAttempts':
    'Zbyt wiele prób logowania. Spróbuj ponownie za chwilę.',
  'login.error.unavailable':
    'Logowanie jest chwilowo niedostępne. Spróbuj ponownie później.',
} as const

export type TranslationKey = keyof typeof polishTranslations

export const spanishTranslations: Record<TranslationKey, string> = {
  'app.loading': 'Cargando la aplicación…',
  'home.eyebrow': 'Frontend preparado',
  'home.title': 'Vista de datos de Subiekt GT',
  'home.description':
    'La base de la aplicación está preparada para la lista y los detalles de productos.',
  'language.label': 'Idioma',
  'language.polish': 'Polaco',
  'language.spanish': 'Español',
  'login.title': 'Subiekt Mobile',
  'login.subtitle': 'Inicio de sesión en el sistema de almacén',
  'login.username': 'Usuario',
  'login.usernamePlaceholder': 'Introduce el nombre de usuario',
  'login.password': 'Contraseña',
  'login.passwordPlaceholder': 'Introduce la contraseña',
  'login.submit': 'Iniciar sesión',
  'login.submitting': 'Iniciando sesión…',
  'login.help': '¿Necesitas ayuda? Contacta con el administrador.',
  'login.error.invalidCredentials': 'El usuario o la contraseña no son correctos.',
  'login.error.tooManyAttempts':
    'Demasiados intentos de inicio de sesión. Vuelve a intentarlo en unos minutos.',
  'login.error.unavailable':
    'El inicio de sesión no está disponible temporalmente. Inténtalo de nuevo más tarde.',
}
