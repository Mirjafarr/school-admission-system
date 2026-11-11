
import { useState } from 'react'
import type { FormEvent } from 'react'
import axios from 'axios'
import {
  ArrowRight, ArrowUpRight, Eye, EyeOff, GraduationCap,
  ShieldCheck, Sparkles, Users, BookOpen, LayoutDashboard,
  LogOut, Menu, X, Clock3, CheckCircle2, Plus,
  RefreshCw, Search, School, AlertCircle, Download
} from 'lucide-react'

const API = 'http://localhost:5069'

type Summary = {
  totalClasses: number
  totalStudents: number
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

type ClassItem = {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
}

type HoldItem = {
  id: string
  classId: string
  studentId: string
  status: string
  expiresAt?: string
  confirmedAt?: string
  className?: string
}

type StudentClass = ClassItem & {
  availableSeats: number
}

type Page = 'overview' | 'classes' | 'students' | 'admissions'

type AdminAdmission = {
  id: string
  classId: string
  className: string
  studentId: string
  status: string
  createdAt: string
  expiresAt: string
  confirmedAt: string | null
}

function App() {
  const [adminAdmissions, setAdminAdmissions] =
    useState<AdminAdmission[]>([])

  const [admissionsLoading, setAdmissionsLoading] =
    useState(false)

  const [admissionsError, setAdmissionsError] = useState('')
  const [admissionFilter, setAdmissionFilter] = useState('ALL')
  const [admissionSearch, setAdmissionSearch] = useState('')

  const [page, setPage] = useState<'login' | 'signup' | 'dashboard' | 'student'>('login')
  const [activePage, setActivePage] = useState<Page>('overview')
const [adminStudents, setAdminStudents] =
  useState<AdminStudent[]>([])

const [studentsLoading, setStudentsLoading] =
  useState(false)

const [studentsError, setStudentsError] = useState('')

const [adminStudentSearch, setAdminStudentSearch] =
  useState('')
  const [studentId, setStudentId] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [token, setToken] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [step, setStep] = useState(1)
  const [summary, setSummary] = useState<Summary | null>(null)
  const [mobileMenu, setMobileMenu] = useState(false)

  const [classes, setClasses] = useState<ClassItem[]>([])
  const [classLoading, setClassLoading] = useState(false)
  const [classError, setClassError] = useState('')
  const [search, setSearch] = useState('')
  const [showAddForm, setShowAddForm] = useState(false)
  const [newClassName, setNewClassName] = useState('')
  const [newTotalSeats, setNewTotalSeats] = useState('')

  // Student portal state
  const [studentHolds, setStudentHolds] = useState<HoldItem[]>([])
  const [studentClasses, setStudentClasses] = useState<StudentClass[]>([])
  const [studentLoading, setStudentLoading] = useState(false)
  const [studentError, setStudentError] = useState('')

  const authHeaders = (accessToken: string) => ({
    Authorization: `Bearer ${accessToken}`
  })

  // ==========================================
  // ADMIN ADMISSIONS
  // ==========================================

  async function loadAdminAdmissions(accessToken: string) {
    if (!accessToken) return

    setAdmissionsLoading(true)
    setAdmissionsError('')

    try {
      const response = await axios.get(`${API}/admin/admissions`, {
        headers: authHeaders(accessToken)
      })

      const rows = Array.isArray(response.data)
        ? response.data
        : response.data?.admissions ?? response.data?.items ?? []

      setAdminAdmissions(rows.map((row: any) => ({
        id: String(row.id),
        classId: row.classId ?? row.class_id ?? '',
        className: row.className ?? row.class_name ?? 'Admission',
        studentId: row.studentId ?? row.student_id ?? '',
        status: String(row.status ?? 'UNKNOWN').toUpperCase(),
        createdAt: row.createdAt ?? row.created_at ?? '',
        expiresAt: row.expiresAt ?? row.expires_at ?? '',
        confirmedAt: row.confirmedAt ?? row.confirmed_at ?? null
      })))
    } catch (err: any) {
      setAdminAdmissions([])
      setAdmissionsError(
        err.response?.data?.error ||
        err.response?.data?.title ||
        'Unable to load admission records. Check that GET /admin/admissions exists and that you are signed in as Admin.'
      )
    } finally {
      setAdmissionsLoading(false)
    }
  }
  async function loadAdminStudents(accessToken: string) {
  if (!accessToken) return

  setStudentsLoading(true)
  setStudentsError('')

  try {
    const response = await axios.get(
      `${API}/admin/students`,
      { headers: authHeaders(accessToken) }
    )

    const rows = Array.isArray(response.data)
      ? response.data
      : response.data?.students ?? []

    setAdminStudents(rows.map((row: any) => ({
      id: String(row.id),
      studentId: row.studentId ?? row.student_id,
      createdAt: row.createdAt ?? row.created_at,
      applicationCount: Number(row.applicationCount ?? 0),
      confirmedAdmissions: Number(row.confirmedAdmissions ?? 0),
      lastApplicationAt:
        row.lastApplicationAt ?? row.last_application_at ?? null,
      latestAdmissionStatus:
        row.latestAdmissionStatus ??
        row.latest_admission_status ??
        null
    })))
  } catch (err: any) {
    setAdminStudents([])
    setStudentsError(
      err.response?.data?.error ||
      'Unable to load registered students.'
    )
  } finally {
    setStudentsLoading(false)
  }
}
function exportAdmissionsCsv() {
  if (adminAdmissions.length === 0) {
    setAdmissionsError('No admission records available to export.')
    return
  }

  const headers = [
    'Student ID',
    'Class Name',
    'Status',
    'Application Date',
    'Expiry Date',
    'Confirmation Date',
    'Admission Reference'
  ]

  const escapeCsv = (value: unknown) =>
    `"${String(value ?? '').replace(/"/g, '""')}"`

  const rows = adminAdmissions.map(item => [
    item.studentId,
    item.className,
    item.status,
    item.createdAt,
    item.expiresAt,
    item.confirmedAt ?? '',
    item.id
  ])

  const csv = '\uFEFF' +
    [headers, ...rows]
      .map(row => row.map(escapeCsv).join(','))
      .join('\r\n')

  const blob = new Blob([csv], {
    type: 'text/csv;charset=utf-8;'
  })

  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = `ADMITLY-Admissions-${new Date()
    .toISOString().slice(0, 10)}.csv`

  document.body.appendChild(link)
  link.click()
  link.remove()

  URL.revokeObjectURL(url)
}
type AdminStudent = {
  id: string
  studentId: string
  createdAt: string
  applicationCount: number
  confirmedAdmissions: number
  lastApplicationAt: string | null
  latestAdmissionStatus: string | null
}

  // ==========================================
  // ADMIN DATA
  // ==========================================

  async function loadDashboard(accessToken: string) {
    const response = await axios.get(`${API}/admin/dashboard`, {
      headers: authHeaders(accessToken)
    })
    setSummary(response.data.summary)
  }

  async function loadClasses(accessToken: string) {
    setClassLoading(true)
    setClassError('')

    try {
      const response = await axios.get(`${API}/classes`, {
        headers: authHeaders(accessToken)
      })

      const rows = Array.isArray(response.data)
        ? response.data
        : response.data.classes ?? []

      setClasses(rows.map((row: any) => ({
        id: row.id,
        name: row.name,
        totalSeats: Number(row.totalSeats ?? row.total_seats ?? 0),
        heldSeats: Number(row.heldSeats ?? row.held_seats ?? 0),
        confirmedSeats: Number(
          row.confirmedSeats ?? row.confirmed_seats ?? 0
        )
      })))
    } catch (err: any) {
      setClassError(
        err.response?.data?.error || 'Could not load classes.'
      )
    } finally {
      setClassLoading(false)
    }
  }

  // ==========================================
  // STUDENT DATA
  // ==========================================

  async function loadStudentPortal(accessToken: string) {
    setStudentLoading(true)
    setStudentError('')

    try {
      const headers = authHeaders(accessToken)

      const [holdsResponse, classesResponse] = await Promise.all([
        axios.get(`${API}/me/holds`, { headers }),
        axios.get(`${API}/classes`, { headers })
      ])

      const holdRows = Array.isArray(holdsResponse.data)
        ? holdsResponse.data
        : holdsResponse.data?.holds ?? holdsResponse.data?.items ?? []

      const classRows = Array.isArray(classesResponse.data)
        ? classesResponse.data
        : classesResponse.data?.classes ?? []

      const normalizedClasses: StudentClass[] = classRows.map((row: any) => {
        const totalSeats = Number(row.totalSeats ?? row.total_seats ?? 0)
        const heldSeats = Number(row.heldSeats ?? row.held_seats ?? 0)
        const confirmedSeats = Number(
          row.confirmedSeats ?? row.confirmed_seats ?? 0
        )

        return {
          id: row.id,
          name: row.name,
          totalSeats,
          heldSeats,
          confirmedSeats,
          availableSeats: Math.max(
            0,
            totalSeats - heldSeats - confirmedSeats
          )
        }
      })

      const classNames = new Map(
        normalizedClasses.map(c => [c.id, c.name])
      )

      setStudentClasses(normalizedClasses)

      setStudentHolds(holdRows.map((hold: any) => ({
        id: hold.id,
        classId: hold.classId ?? hold.class_id,
        studentId: hold.studentId ?? hold.student_id,
        status: String(hold.status ?? 'UNKNOWN').toUpperCase(),
        expiresAt: hold.expiresAt ?? hold.expires_at,
        confirmedAt: hold.confirmedAt ?? hold.confirmed_at,
        className: hold.className ??
          hold.class_name ??
          classNames.get(hold.classId ?? hold.class_id) ??
          'Admission'
      })))
    } catch (err: any) {
      setStudentError(
        err.response?.data?.error ||
        'Could not load your admission information. Please refresh and try again.'
      )
    } finally {
      setStudentLoading(false)
    }
  }

  async function reserveSeat(classId: string) {
    setStudentError('')
    setStudentLoading(true)

    try {
      await axios.post(
        `${API}/classes/${classId}/holds`,
        {},
        { headers: authHeaders(token) }
      )

      await loadStudentPortal(token)
    } catch (err: any) {
      setStudentError(
        err.response?.data?.error ||
        'Could not reserve this seat. Please check availability.'
      )
    } finally {
      setStudentLoading(false)
    }
  }

  async function confirmAdmission(classId: string, holdId: string) {
    setStudentError('')
    setStudentLoading(true)

    try {
      await axios.post(
        `${API}/classes/${classId}/holds/${holdId}/confirm`,
        {},
        { headers: authHeaders(token) }
      )

      await loadStudentPortal(token)
    } catch (err: any) {
      setStudentError(
        err.response?.data?.error ||
        'Could not confirm this admission. The hold may have expired.'
      )
    } finally {
      setStudentLoading(false)
    }
  }

  // ==========================================
  // AUTHENTICATION
  // ==========================================

  async function login(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      const response = await axios.post(`${API}/auth/login`, {
        studentId: studentId.trim(),
        password
      })

      const accessToken = response.data.accessToken
      const payload = JSON.parse(atob(accessToken.split('.')[1]))

      const role =
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']

      sessionStorage.setItem('admitly_token', accessToken)
      setToken(accessToken)

      if (role === 'Admin') {
        await Promise.all([
          loadDashboard(accessToken),
          loadClasses(accessToken)
        ])

        setActivePage('overview')
        setPage('dashboard')
      } else if (role === 'Student') {
        await loadStudentPortal(accessToken)
        setPage('student')
      } else {
        sessionStorage.removeItem('admitly_token')
        setToken('')
        setError(
          'Your account role is not recognized. Please contact the administrator.'
        )
      }
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
        'Login failed. Check your API connection and credentials.'
      )
    } finally {
      setLoading(false)
    }
  }

  async function register(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setError('')
    setLoading(true)

    try {
      await axios.post(`${API}/auth/register`, {
        studentId: studentId.trim(),
        password
      })

      setError('Account created successfully! Please sign in.')
      setPage('login')
      setPassword('')
      setStep(1)
    } catch (err: any) {
      setError(
        err.response?.data?.error ||
        'Registration failed. Please check your information.'
      )
    } finally {
      setLoading(false)
    }
  }

  // ==========================================
  // ADMIN ACTIONS
  // ==========================================

  async function refreshDashboard() {
    try {
      setError('')

      const refreshTasks = [
        loadDashboard(token),
        loadClasses(token)
      ]

      if (activePage === 'admissions') {
        refreshTasks.push(loadAdminAdmissions(token))
      }
      if (activePage === 'students') {
  refreshTasks.push(loadAdminStudents(token))
}

      await Promise.all(refreshTasks)
    } catch {
      setError('Could not refresh dashboard data.')
    }
  }

  async function createClass(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setClassError('')

    const seats = Number(newTotalSeats)

    if (
      !newClassName.trim() ||
      !Number.isInteger(seats) ||
      seats < 1
    ) {
      setClassError(
        'Enter a valid class name and a positive whole number of seats.'
      )
      return
    }

    setClassLoading(true)

    try {
      await axios.post(
        `${API}/classes`,
        {
          name: newClassName.trim(),
          totalSeats: seats
        },
        { headers: authHeaders(token) }
      )

      setNewClassName('')
      setNewTotalSeats('')
      setShowAddForm(false)

      await Promise.all([
        loadClasses(token),
        loadDashboard(token)
      ])
    } catch (err: any) {
      setClassError(
        err.response?.data?.error ||
        'Could not create class. Check the API response.'
      )
    } finally {
      setClassLoading(false)
    }
  }

  function navigate(target: Page) {
    setActivePage(target)
    setMobileMenu(false)
    setError('')

    if (target === 'classes') {
      loadClasses(token)
    }

    if (target === 'overview') {
      loadDashboard(token)
    }

    if (target === 'admissions') {
      loadAdminAdmissions(token)
    }
      if (target === 'students') {
    loadAdminStudents(token)
  }
  }

  function logout() {
    sessionStorage.removeItem('admitly_token')

    setToken('')
    setSummary(null)
    setClasses([])
    setStudentHolds([])
    setStudentClasses([])
    setStudentError('')
    setAdminAdmissions([])
    setAdmissionsError('')
    setAdmissionsLoading(false)
    setAdmissionFilter('ALL')
    setAdmissionSearch('')
    setPassword('')
    setPage('login')
  }

  const filteredClasses = classes.filter(c =>
    c.name.toLowerCase().includes(search.toLowerCase())
  )
  const filteredAdminStudents = adminStudents.filter(student =>
  student.studentId
    .toLowerCase()
    .includes(adminStudentSearch.toLowerCase())
)
  // ==========================================
  // STUDENT PORTAL
  // ==========================================

  if (page === 'student') {
    const confirmedCount = studentHolds.filter(
      h => h.status === 'CONFIRMED'
    ).length

    const activeHolds = studentHolds.filter(
      h => h.status === 'HELD'
    )

    return (
      <div className="min-h-screen bg-[#0B1220] text-[#F3F4EE]">

        <header className="border-b border-white/10 bg-[#121D2E]/90">
          <div className="max-w-7xl mx-auto px-5 md:px-10 py-5 flex items-center justify-between">
            <Logo />

            <div className="flex items-center gap-4">
              <span className="hidden sm:inline text-xs mono-font tracking-widest text-[#C6F36B]">
                STUDENT PORTAL
              </span>

              <button
                onClick={logout}
                className="flex items-center gap-2 text-sm text-[#94A3B8] hover:text-[#C6F36B] transition"
              >
                <LogOut size={17} />
                Sign out
              </button>
            </div>
          </div>
        </header>

        <main className="max-w-7xl mx-auto px-5 md:px-10 py-8 md:py-12">

          {/* HERO */}

          <section className="relative overflow-hidden rounded-[28px] bg-[#121D2E] border border-white/10 p-7 md:p-10 mb-8">
            <div className="absolute -right-16 -top-24 w-80 h-80 rounded-full border border-[#C6F36B]/15" />
            <div className="absolute -right-5 -top-14 w-56 h-56 rounded-full border border-[#C6F36B]/15" />

            <div className="relative z-10 max-w-2xl">
              <p className="mono-font text-xs text-[#C6F36B] tracking-[.2em]">
                YOUR CAMPUS / YOUR FUTURE
              </p>

              <h1 className="display-font text-4xl md:text-6xl font-bold mt-4 leading-tight">
                Your next chapter.
              </h1>

              <p className="text-[#94A3B8] mt-4 max-w-xl leading-relaxed">
                Explore available classes, reserve your seat and follow your admission journey.
              </p>
            </div>

            <GraduationCap
              className="absolute right-10 bottom-8 text-[#C6F36B]/60 hidden md:block"
              size={76}
              strokeWidth={1}
            />
          </section>

          {/* ERRORS */}

          {studentError && (
            <div className="bg-red-950/40 border border-red-500/20 text-red-300 p-4 rounded-xl mb-6 flex items-start gap-3">
              <AlertCircle size={18} className="shrink-0 mt-0.5" />
              <span>{studentError}</span>
            </div>
          )}

          {/* STUDENT STATISTICS */}

          <section className="grid sm:grid-cols-3 gap-4 mb-10">
            {[
              {
                label: 'My applications',
                value: studentHolds.length,
                icon: BookOpen
              },
              {
                label: 'Confirmed admissions',
                value: confirmedCount,
                icon: CheckCircle2
              },
              {
                label: 'Active reservations',
                value: activeHolds.length,
                icon: Clock3
              }
            ].map(item => (
              <div
                key={item.label}
                className="bg-[#121D2E] border border-white/10 rounded-2xl p-6"
              >
                <div className="flex items-center justify-between">
                  <p className="text-sm text-[#94A3B8]">
                    {item.label}
                  </p>

                  <item.icon
                    size={20}
                    className="text-[#C6F36B]"
                  />
                </div>

                <p className="display-font text-4xl font-bold mt-5">
                  {item.value}
                </p>
              </div>
            ))}
          </section>

          {/* MY APPLICATIONS */}

          <section className="mb-10">
            <div className="flex flex-wrap items-center justify-between gap-4 mb-5">
              <div>
                <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
                  ADMISSION RECORD
                </p>

                <h2 className="display-font text-2xl md:text-3xl font-bold mt-2">
                  My applications
                </h2>
              </div>

              <button
                onClick={() => loadStudentPortal(token)}
                disabled={studentLoading}
                className="px-4 py-3 rounded-xl bg-[#1B2A40] border border-white/10 text-sm flex items-center gap-2 hover:border-[#C6F36B]/40 disabled:opacity-50"
              >
                <RefreshCw
                  size={16}
                  className={studentLoading ? 'animate-spin' : ''}
                />
                Refresh
              </button>
            </div>

            {studentLoading && studentHolds.length === 0 ? (
              <div className="bg-[#121D2E] rounded-2xl border border-white/10 p-10 text-center text-[#94A3B8]">
                Loading your admission records...
              </div>
            ) : studentHolds.length === 0 ? (
              <div className="bg-[#121D2E] rounded-2xl border border-white/10 p-10 text-center">
                <BookOpen
                  size={34}
                  className="mx-auto text-[#C6F36B] mb-4"
                />

                <h3 className="font-bold text-lg">
                  No applications yet
                </h3>

                <p className="text-sm text-[#94A3B8] mt-2">
                  Choose a class below to start your admission process.
                </p>
              </div>
            ) : (
              <div className="grid md:grid-cols-2 gap-4">
                {studentHolds.map(hold => (
                  <article
                    key={hold.id}
                    className="bg-[#121D2E] border border-white/10 rounded-2xl p-6"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="mono-font text-[10px] text-[#94A3B8] tracking-widest">
                          APPLICATION
                        </p>

                        <h3 className="display-font text-xl font-bold mt-2">
                          {hold.className || 'Admission'}
                        </h3>
                      </div>

                      <span
                        className={`text-[10px] font-bold tracking-wider rounded-full px-3 py-2 ${
                          hold.status === 'CONFIRMED'
                            ? 'bg-[#C6F36B]/10 text-[#C6F36B]'
                            : hold.status === 'HELD'
                            ? 'bg-[#7185FF]/15 text-[#9AA8FF]'
                            : 'bg-white/10 text-[#94A3B8]'
                        }`}
                      >
                        {hold.status}
                      </span>
                    </div>

                    <p className="text-xs text-[#94A3B8] mt-4 break-all">
                      Reference: {hold.id}
                    </p>

                    {hold.expiresAt && hold.status === 'HELD' && (
                      <p className="text-xs text-[#E7B89C] mt-3">
                        Expires: {new Date(hold.expiresAt).toLocaleString()}
                      </p>
                    )}

                    {hold.confirmedAt && (
                      <p className="text-xs text-[#94A3B8] mt-3">
                        Confirmed: {new Date(hold.confirmedAt).toLocaleString()}
                      </p>
                    )}

                    {hold.status === 'HELD' && (
                      <button
                        disabled={studentLoading}
                        onClick={() => confirmAdmission(hold.classId, hold.id)}
                        className="lime-button w-full mt-5 disabled:opacity-50"
                      >
                        {studentLoading
                          ? 'Please wait...'
                          : 'Confirm admission'}
                      </button>
                    )}
                  </article>
                ))}
              </div>
            )}
          </section>

          {/* AVAILABLE CLASSES */}

          <section>
            <div className="mb-5">
              <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
                FIND YOUR PLACE
              </p>

              <h2 className="display-font text-2xl md:text-3xl font-bold mt-2">
                Explore classes
              </h2>

              <p className="text-sm text-[#94A3B8] mt-2">
                Choose an available class to reserve a seat.
              </p>
            </div>

            {studentClasses.length === 0 ? (
              <div className="bg-[#121D2E] border border-white/10 rounded-2xl p-8 text-[#94A3B8]">
                No classes are currently available.
              </div>
            ) : (
              <div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {studentClasses.map(c => (
                  <article
                    key={c.id}
                    className="bg-[#121D2E] border border-white/10 rounded-2xl p-6 hover:border-[#C6F36B]/30 transition"
                  >
                    <div className="w-11 h-11 rounded-xl bg-[#C6F36B]/10 text-[#C6F36B] flex items-center justify-center mb-5">
                      <School size={22} />
                    </div>

                    <h3 className="display-font text-xl font-bold">
                      {c.name}
                    </h3>

                    <p className="text-sm text-[#94A3B8] mt-3">
                      {c.availableSeats} of {c.totalSeats} seats available
                    </p>

                    <div className="h-2 rounded-full bg-[#1B2A40] mt-4 overflow-hidden">
                      <div
                        className="h-full bg-[#C6F36B] rounded-full"
                        style={{
                          width: `${
                            c.totalSeats
                              ? (c.availableSeats / c.totalSeats) * 100
                              : 0
                          }%`
                        }}
                      />
                    </div>

                    <button
                      disabled={c.availableSeats <= 0 || studentLoading}
                      onClick={() => reserveSeat(c.id)}
                      className="lime-button w-full mt-6 disabled:opacity-40"
                    >
                      {c.availableSeats <= 0
                        ? 'Class full'
                        : 'Reserve a seat'}
                    </button>
                  </article>
                ))}
              </div>
            )}
          </section>

          <footer className="mt-12 pt-6 border-t border-white/10 flex flex-wrap justify-between gap-3 text-xs text-[#94A3B8]/50 mono-font">
            <span>ADMITLY © 2026</span>
            <span>YOUR FUTURE, IN MOTION ↗</span>
          </footer>
        </main>
      </div>
    )
  }

  // ==========================================
  // ADMIN APPLICATION
  // ==========================================

  if (page === 'dashboard') {
    const metrics = [
      { label: 'Total students', value: summary?.totalStudents, icon: Users },
      { label: 'Total classes', value: summary?.totalClasses, icon: BookOpen },
      { label: 'Total seats', value: summary?.totalSeats, icon: GraduationCap },
      { label: 'Held seats', value: summary?.heldSeats, icon: Clock3 },
      { label: 'Confirmed seats', value: summary?.confirmedSeats, icon: CheckCircle2 },
      { label: 'Available seats', value: summary?.availableSeats, icon: ArrowUpRight }
    ]

    const confirmedPercentage = summary?.totalSeats
      ? (summary.confirmedSeats / summary.totalSeats) * 100
      : 0

    const heldPercentage = summary?.totalSeats
      ? (summary.heldSeats / summary.totalSeats) * 100
      : 0

    return (
      <div className="min-h-screen bg-[#0B1220] text-[#F3F4EE] md:flex">

        {/* SIDEBAR */}

        <aside
          className={`${
            mobileMenu ? 'flex' : 'hidden'
          } md:flex fixed md:sticky top-0 inset-y-0 left-0 z-30 w-72 h-screen bg-[#121D2E] border-r border-white/10 p-7 flex-col`}
        >
          <div className="flex items-center justify-between mb-14">
            <Logo />

            <button
              className="md:hidden"
              onClick={() => setMobileMenu(false)}
            >
              <X />
            </button>
          </div>

          <p className="mono-font text-xs text-[#94A3B8]/50 mb-5 tracking-widest">
            WORKSPACE
          </p>

          <nav className="space-y-2">
            <NavItem
              icon={<LayoutDashboard size={19} />}
              label="Overview"
              active={activePage === 'overview'}
              onClick={() => navigate('overview')}
            />

            <NavItem
              icon={<Users size={19} />}
              label="Students"
              active={activePage === 'students'}
              onClick={() => navigate('students')}
            />

            <NavItem
              icon={<BookOpen size={19} />}
              label="Classes"
              active={activePage === 'classes'}
              onClick={() => navigate('classes')}
            />

            <NavItem
              icon={<GraduationCap size={19} />}
              label="Admissions"
              active={activePage === 'admissions'}
              onClick={() => navigate('admissions')}
            />
          </nav>

          <div className="mt-auto glass rounded-2xl p-4">
            <ShieldCheck className="text-[#C6F36B] mb-3" />

            <p className="font-bold">Admin access</p>

            <p className="text-xs text-[#94A3B8] mt-1">
              Secure management workspace
            </p>

            <div className="flex items-center gap-2 mt-4">
              <span className="w-2 h-2 rounded-full bg-[#C6F36B] animate-pulse" />

              <span className="text-[10px] text-[#C6F36B] mono-font">
                SYSTEM ONLINE
              </span>
            </div>
          </div>

          <button
            onClick={logout}
            className="mt-5 p-3 text-[#94A3B8] flex gap-3 items-center hover:text-[#C6F36B] transition"
          >
            <LogOut size={18} />
            Sign out
          </button>
        </aside>

        {mobileMenu && (
          <div
            className="fixed inset-0 bg-black/60 z-20 md:hidden"
            onClick={() => setMobileMenu(false)}
          />
        )}

        {/* MAIN CONTENT */}

        <main className="flex-1 min-w-0 p-5 md:p-10 lg:p-12">

          <header className="flex justify-between items-center mb-10">
            <button
              className="md:hidden text-[#F3F4EE]"
              onClick={() => setMobileMenu(true)}
            >
              <Menu />
            </button>

            <div>
              <p className="mono-font text-xs text-[#C6F36B]/70 tracking-widest">
                ADMINISTRATION / {activePage.toUpperCase()}
              </p>

              <h1 className="display-font text-3xl md:text-4xl font-bold mt-2">
                {activePage === 'overview' && 'The overview.'}
                {activePage === 'classes' && 'Class management.'}
                {activePage === 'students' && 'Student management.'}
                {activePage === 'admissions' && 'Admissions.'}
              </h1>

              <p className="text-sm text-[#94A3B8] mt-2">
                {activePage === 'overview'
                  ? "Welcome back. Here's what's happening today."
                  : `Manage your ${activePage} in one place.`}
              </p>
            </div>

            <button
              onClick={refreshDashboard}
              className="bg-[#121D2E] border border-white/10 hover:border-[#C6F36B]/40 rounded-xl px-4 py-3 text-sm font-bold transition flex items-center gap-2"
            >
              <RefreshCw size={15} />
              <span className="hidden sm:inline">Refresh</span>
            </button>
          </header>

          {error && (
            <div className="bg-red-950/40 border border-red-500/20 text-red-300 p-4 rounded-xl mb-5">
              {error}
            </div>
          )}

          {/* OVERVIEW PAGE */}

          {activePage === 'overview' && (
            <>
              <section className="bg-[#121D2E] border border-white/10 rounded-[28px] p-7 md:p-10 relative overflow-hidden mb-8">
                <div className="absolute -right-12 -top-24 w-72 h-72 border border-[#C6F36B]/20 rounded-full" />
                <div className="absolute -right-4 -top-16 w-52 h-52 border border-[#C6F36B]/20 rounded-full" />

                <div className="relative z-10">
                  <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
                    ADMITLY / CONTROL CENTER
                  </p>

                  <h2 className="display-font text-3xl md:text-5xl font-bold mt-5 max-w-xl leading-tight">
                    Every student. Every opportunity. One place.
                  </h2>

                  <p className="text-[#94A3B8] mt-4">
                    Your admission ecosystem, at a glance.
                  </p>
                </div>

                <Sparkles
                  className="absolute right-12 bottom-12 text-[#C6F36B] hidden md:block"
                  size={50}
                />
              </section>

              <div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {metrics.map(item => (
                  <div
                    key={item.label}
                    className="bg-[#121D2E] p-6 rounded-2xl border border-white/10 hover:border-[#C6F36B]/30 hover:-translate-y-1 transition duration-300"
                  >
                    <div className="flex justify-between items-center">
                      <span className="text-sm text-[#94A3B8]">
                        {item.label}
                      </span>

                      <item.icon
                        className="text-[#C6F36B]"
                        size={20}
                      />
                    </div>

                    <p className="display-font text-4xl font-bold mt-6">
                      {item.value ?? '—'}
                    </p>

                    <p className="mono-font text-[10px] text-[#94A3B8]/50 mt-2 tracking-wider">
                      LIVE DATABASE METRIC
                    </p>
                  </div>
                ))}
              </div>

              <div className="bg-[#121D2E] rounded-2xl p-7 mt-6 border border-white/10">
                <div className="flex justify-between items-center">
                  <div>
                    <p className="text-sm text-[#94A3B8]">
                      Seat availability
                    </p>

                    <h3 className="display-font text-2xl font-bold mt-1">
                      Campus capacity
                    </h3>
                  </div>

                  <GraduationCap
                    className="text-[#C6F36B]"
                    size={30}
                  />
                </div>

                <div className="h-4 bg-[#1B2A40] rounded-full overflow-hidden mt-7 flex">
                  <div
                    className="bg-[#C6F36B] transition-all"
                    style={{ width: `${confirmedPercentage}%` }}
                  />

                  <div
                    className="bg-[#7185FF] transition-all"
                    style={{ width: `${heldPercentage}%` }}
                  />
                </div>

                <div className="flex flex-wrap gap-6 text-xs mt-5 text-[#94A3B8]">
                  <span>🟢 Confirmed</span>
                  <span>🔵 Held</span>
                  <span>Available</span>
                </div>

                <p className="mt-5 text-sm text-[#94A3B8]">
                  {summary?.availableSeats ?? '—'} seats currently available.
                </p>
              </div>
            </>
          )}

          {/* CLASSES PAGE */}

          {activePage === 'classes' && (
            <section>
              <div className="bg-[#121D2E] border border-white/10 rounded-2xl p-6 md:p-8 mb-6">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div>
                    <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
                      ACADEMIC STRUCTURE
                    </p>

                    <h2 className="display-font text-2xl font-bold mt-2">
                      Your classes
                    </h2>

                    <p className="text-sm text-[#94A3B8] mt-1">
                      Create and manage class capacity.
                    </p>
                  </div>

                  <button
                    onClick={() => {
                      setShowAddForm(!showAddForm)
                      setClassError('')
                    }}
                    className="lime-button flex items-center justify-center gap-2"
                  >
                    <Plus size={18} />
                    Add new class
                  </button>
                </div>
              </div>

              {/* ADD CLASS FORM */}

              {showAddForm && (
                <form
                  onSubmit={createClass}
                  className="bg-[#121D2E] border border-[#C6F36B]/30 rounded-2xl p-6 mb-6"
                >
                  <h3 className="display-font text-xl font-bold mb-5">
                    Create a new class
                  </h3>

                  <div className="grid md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm text-[#94A3B8] mb-2">
                        Class name
                      </label>

                      <input
                        className="auth-input"
                        placeholder="e.g. Class 8 - Morning Shift"
                        value={newClassName}
                        onChange={e => setNewClassName(e.target.value)}
                        required
                      />
                    </div>

                    <div>
                      <label className="block text-sm text-[#94A3B8] mb-2">
                        Total seats
                      </label>

                      <input
                        className="auth-input"
                        type="number"
                        min="1"
                        step="1"
                        placeholder="e.g. 100"
                        value={newTotalSeats}
                        onChange={e => setNewTotalSeats(e.target.value)}
                        required
                      />
                    </div>
                  </div>

                  {classError && (
                    <p className="text-sm text-red-300 mt-4">
                      {classError}
                    </p>
                  )}

                  <div className="flex gap-3 mt-6">
                    <button
                      disabled={classLoading}
                      className="lime-button disabled:opacity-50"
                    >
                      {classLoading ? 'Creating...' : 'Create class'}
                    </button>

                    <button
                      type="button"
                      onClick={() => setShowAddForm(false)}
                      className="px-5 py-3 rounded-xl bg-[#1B2A40] text-[#94A3B8] hover:text-white"
                    >
                      Cancel
                    </button>
                  </div>
                </form>
              )}

              {/* CLASS SEARCH */}

              <div className="flex flex-col sm:flex-row gap-3 mb-5">
                <div className="relative flex-1">
                  <Search
                    className="absolute left-4 top-1/2 -translate-y-1/2 text-[#94A3B8]"
                    size={18}
                  />

                  <input
                    className="auth-input pl-11"
                    placeholder="Search classes..."
                    value={search}
                    onChange={e => setSearch(e.target.value)}
                  />
                </div>

                <button
                  onClick={() => loadClasses(token)}
                  className="px-5 py-3 rounded-xl bg-[#1B2A40] border border-white/10 text-[#F3F4EE] hover:border-[#C6F36B]/40"
                >
                  <RefreshCw size={17} />
                </button>
              </div>

              {classError && !showAddForm && (
                <div className="bg-red-950/40 border border-red-500/20 text-red-300 p-4 rounded-xl mb-5">
                  {classError}
                </div>
              )}

              {/* CLASS TABLE */}

              <div className="bg-[#121D2E] border border-white/10 rounded-2xl overflow-hidden">
                <div className="p-5 border-b border-white/10 flex justify-between">
                  <h3 className="font-bold">All classes</h3>

                  <span className="text-xs text-[#C6F36B] mono-font">
                    {classes.length} RECORDS
                  </span>
                </div>

                {classLoading ? (
                  <div className="p-12 text-center text-[#94A3B8]">
                    Loading classes...
                  </div>
                ) : filteredClasses.length === 0 ? (
                  <div className="p-12 text-center">
                    <School
                      size={36}
                      className="mx-auto text-[#C6F36B] mb-4"
                    />

                    <p className="font-bold">No classes found</p>

                    <p className="text-sm text-[#94A3B8] mt-2">
                      Create a class or change your search.
                    </p>
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-sm">
                      <thead className="bg-[#1B2A40] text-[#94A3B8]">
                        <tr>
                          <th className="p-4">Class name</th>
                          <th className="p-4">Total seats</th>
                          <th className="p-4">Held</th>
                          <th className="p-4">Confirmed</th>
                          <th className="p-4">Available</th>
                        </tr>
                      </thead>

                      <tbody>
                        {filteredClasses.map(c => (
                          <tr
                            key={c.id}
                            className="border-t border-white/5 hover:bg-white/[.025]"
                          >
                            <td className="p-4 font-semibold text-[#F3F4EE]">
                              {c.name}

                              <p className="text-[10px] text-[#94A3B8]/50 mt-1">
                                {c.id}
                              </p>
                            </td>

                            <td className="p-4">{c.totalSeats}</td>
                            <td className="p-4 text-[#7185FF]">
                              {c.heldSeats}
                            </td>
                            <td className="p-4 text-[#C6F36B]">
                              {c.confirmedSeats}
                            </td>

                            <td className="p-4 font-bold">
                              {Math.max(
                                0,
                                c.totalSeats - c.heldSeats - c.confirmedSeats
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </section>
          )}

          
{/* STUDENTS PAGE */}

{activePage === 'students' && (
  <section className="space-y-6">

    {/* STATISTICS */}

    <div className="grid sm:grid-cols-3 gap-4">

      {[
        {
          label: 'Registered students',
          value: adminStudents.length
        },
        {
          label: 'Students with applications',
          value: adminStudents.filter(
            s => s.applicationCount > 0
          ).length
        },
        {
          label: 'Admitted students',
          value: adminStudents.filter(
            s => s.confirmedAdmissions > 0
          ).length
        }
      ].map(item => (
        <div
          key={item.label}
          className="bg-[#121D2E] border border-white/10 rounded-2xl p-6"
        >
          <p className="text-sm text-[#94A3B8]">
            {item.label}
          </p>

          <p className="display-font text-4xl font-bold mt-4">
            {item.value}
          </p>

          <p className="mono-font text-[10px] text-[#94A3B8]/50 mt-2">
            LIVE DATABASE RECORD
          </p>
        </div>
      ))}

    </div>

    {/* STUDENT DIRECTORY */}

    <div className="bg-[#121D2E] border border-white/10 rounded-2xl p-6 md:p-8">

      <div className="flex flex-wrap justify-between items-center gap-4 mb-6">

        <div>
          <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
            STUDENT DIRECTORY
          </p>

          <h2 className="display-font text-2xl font-bold mt-2">
            Registered students
          </h2>

          <p className="text-sm text-[#94A3B8] mt-2">
            Manage and monitor student registration and admission activity.
          </p>
        </div>

        <button
          onClick={() => loadAdminStudents(token)}
          disabled={studentsLoading}
          className="px-5 py-3 rounded-xl bg-[#1B2A40] border border-white/10 text-[#C6F36B] flex items-center gap-2"
        >
          <RefreshCw
            size={16}
            className={studentsLoading ? 'animate-spin' : ''}
          />
          Refresh students
        </button>

      </div>

      {/* SEARCH */}

      <div className="mb-6">

        <label className="block text-xs text-[#94A3B8] mb-2">
          Search by Student ID
        </label>

        <input
          className="auth-input"
          placeholder="Enter student ID..."
          value={adminStudentSearch}
          onChange={e => setAdminStudentSearch(e.target.value)}
        />

      </div>

      {/* ERROR */}

      {studentsError && (
        <div className="bg-red-950/40 border border-red-500/20 text-red-300 p-4 rounded-xl mb-5">
          {studentsError}
        </div>
      )}

      {/* LOADING */}

      {studentsLoading && adminStudents.length === 0 ? (
        <div className="p-12 text-center text-[#94A3B8]">
          Loading registered students...
        </div>
      ) : filteredAdminStudents.length === 0 ? (
        <div className="p-12 text-center text-[#94A3B8]">
          <Users
            size={35}
            className="mx-auto text-[#C6F36B] mb-4"
          />

          <p className="font-bold">
            No students found
          </p>

          <p className="text-sm mt-2">
            Try another search or register a student account.
          </p>
        </div>
      ) : (

        <div className="overflow-x-auto">

          <table className="w-full text-left text-sm">

            <thead className="bg-[#1B2A40] text-[#94A3B8]">

              <tr>
                <th className="p-4">Student ID</th>
                <th className="p-4">Registered</th>
                <th className="p-4">Applications</th>
                <th className="p-4">Confirmed</th>
                <th className="p-4">Latest status</th>
              </tr>

            </thead>

            <tbody>

              {filteredAdminStudents.map(student => {

                const status =
                  student.latestAdmissionStatus || 'REGISTERED'

                const statusStyle =
                  status === 'CONFIRMED'
                    ? 'bg-[#C6F36B]/10 text-[#C6F36B]'
                    : status === 'HELD'
                    ? 'bg-[#7185FF]/15 text-[#9AA8FF]'
                    : 'bg-white/10 text-[#94A3B8]'

                return (
                  <tr
                    key={student.id}
                    className="border-t border-white/5 hover:bg-white/[.025]"
                  >

                    <td className="p-4 font-semibold text-[#F3F4EE]">
                      {student.studentId}
                    </td>

                    <td className="p-4 text-[#94A3B8]">
                      {student.createdAt
                        ? new Date(student.createdAt).toLocaleDateString()
                        : '—'}
                    </td>

                    <td className="p-4">
                      {student.applicationCount}
                    </td>

                    <td className="p-4 text-[#C6F36B]">
                      {student.confirmedAdmissions}
                    </td>

                    <td className="p-4">
                      <span className={`px-3 py-2 rounded-full text-[10px] font-bold ${statusStyle}`}>
                        {status}
                      </span>
                    </td>

                  </tr>
                )
              })}

            </tbody>

          </table>

        </div>

      )}

    </div>

  </section>
)}


          {/* ADMISSIONS PAGE */}

          {activePage === 'admissions' && (
            <section className="space-y-6">
              <div className="grid sm:grid-cols-2 xl:grid-cols-3 gap-4">
                {[
                  {
                    label: 'Total applications',
                    value: adminAdmissions.length
                  },
                  {
                    label: 'Confirmed admissions',
                    value: adminAdmissions.filter(
                      item => item.status === 'CONFIRMED'
                    ).length
                  },
                  {
                    label: 'Active reservations',
                    value: adminAdmissions.filter(
                      item => item.status === 'HELD'
                    ).length
                  }
                ].map(item => (
                  <div
                    key={item.label}
                    className="bg-[#121D2E] border border-white/10 rounded-2xl p-6"
                  >
                    <p className="text-sm text-[#94A3B8]">{item.label}</p>
                    <p className="display-font text-4xl font-bold mt-4">
                      {item.value}
                    </p>
                    <p className="mono-font text-[10px] text-[#94A3B8]/50 mt-2 tracking-wider">
                      LIVE DATABASE RECORD
                    </p>
                  </div>
                ))}
              </div>

              <div className="bg-[#121D2E] border border-white/10 rounded-2xl p-6 md:p-8">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
                  <div>
                    <p className="mono-font text-xs text-[#C6F36B] tracking-widest">
                      ADMISSION MANAGEMENT
                    </p>
                    <h2 className="display-font text-2xl font-bold mt-2">
                      All admission records
                    </h2>
                    <p className="text-sm text-[#94A3B8] mt-2">
                      View reservations and confirmed admissions from all students.
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={() => loadAdminAdmissions(token)}
                    disabled={admissionsLoading}
                    className="px-5 py-3 rounded-xl bg-[#1B2A40] border border-white/10 text-[#C6F36B] hover:border-[#C6F36B]/40 disabled:opacity-50 flex items-center justify-center gap-2"
                  >
                    <RefreshCw
                      size={16}
                      className={admissionsLoading ? 'animate-spin' : ''}
                    />

<button
  type="button"
  onClick={exportAdmissionsCsv}
  disabled={adminAdmissions.length === 0}
  className="px-5 py-3 rounded-xl bg-[#C6F36B] text-[#0B1220] font-bold flex items-center justify-center gap-2 hover:bg-[#D5FF80] disabled:opacity-40 disabled:cursor-not-allowed"
>
  <Download size={17} />
  Export CSV
</button>

                    Refresh records
                  </button>
                </div>

                <div className="grid md:grid-cols-2 gap-3 mb-6">
                  <div>
                    <label className="block text-xs text-[#94A3B8] mb-2">
                      Search
                    </label>
                    <input
                      className="auth-input"
                      placeholder="Search student ID or class..."
                      value={admissionSearch}
                      onChange={e => setAdmissionSearch(e.target.value)}
                    />
                  </div>

                  <div>
                    <label className="block text-xs text-[#94A3B8] mb-2">
                      Filter by status
                    </label>
                    <select
                      className="auth-input"
                      value={admissionFilter}
                      onChange={e => setAdmissionFilter(e.target.value)}
                    >
                      <option value="ALL">All statuses</option>
                      <option value="HELD">Held</option>
                      <option value="CONFIRMED">Confirmed</option>
                      <option value="EXPIRED">Expired</option>
                      <option value="CANCELLED">Cancelled</option>
                    </select>
                  </div>
                </div>

                {admissionsError && (
                  <div className="bg-red-950/40 border border-red-500/20 text-red-300 p-4 rounded-xl mb-5">
                    {admissionsError}
                  </div>
                )}

                {admissionsLoading && adminAdmissions.length === 0 ? (
                  <div className="p-12 text-center text-[#94A3B8]">
                    Loading admission records...
                  </div>
                ) : (
                  (() => {
                    const visibleAdmissions = adminAdmissions
                      .filter(item =>
                        admissionFilter === 'ALL' ||
                        item.status === admissionFilter
                      )
                      .filter(item =>
                        `${item.studentId} ${item.className}`
                          .toLowerCase()
                          .includes(admissionSearch.trim().toLowerCase())
                      )

                    if (visibleAdmissions.length === 0) {
                      return (
                        <div className="p-12 text-center text-[#94A3B8] border border-white/5 rounded-xl">
                          {adminAdmissions.length === 0
                            ? 'No admission records found.'
                            : 'No records match your search or status filter.'}
                        </div>
                      )
                    }

                    return (
                      <div className="overflow-x-auto">
                        <table className="w-full text-left text-sm">
                          <thead className="bg-[#1B2A40] text-[#94A3B8]">
                            <tr>
                              <th className="p-4">Student ID</th>
                              <th className="p-4">Class</th>
                              <th className="p-4">Status</th>
                              <th className="p-4">Applied</th>
                              <th className="p-4">Confirmed</th>
                            </tr>
                          </thead>
                          <tbody>
                            {visibleAdmissions.map(item => (
                              <tr
                                key={item.id}
                                className="border-t border-white/5 hover:bg-white/[.025]"
                              >
                                <td className="p-4 font-semibold">
                                  {item.studentId || '—'}
                                </td>
                                <td className="p-4">
                                  {item.className}
                                </td>
                                <td className="p-4">
                                  <span
                                    className={`inline-flex rounded-full px-3 py-2 text-[10px] font-bold tracking-wider ${
                                      item.status === 'CONFIRMED'
                                        ? 'bg-[#C6F36B]/10 text-[#C6F36B]'
                                        : item.status === 'HELD'
                                        ? 'bg-[#7185FF]/15 text-[#9AA8FF]'
                                        : 'bg-white/10 text-[#94A3B8]'
                                    }`}
                                  >
                                    {item.status}
                                  </span>
                                </td>
                                <td className="p-4 text-[#94A3B8]">
                                  {item.createdAt
                                    ? new Date(item.createdAt).toLocaleString()
                                    : '—'}
                                </td>
                                <td className="p-4 text-[#94A3B8]">
                                  {item.confirmedAt
                                    ? new Date(item.confirmedAt).toLocaleString()
                                    : '—'}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    )
                  })()
                )}
              </div>
            </section>
          )}

          <footer className="mt-10 pt-6 border-t border-white/10 flex flex-wrap justify-between gap-3 text-xs text-[#94A3B8]/50 mono-font">
            <span>ADMITLY © 2026</span>
            <span>BUILT FOR THE NEXT GENERATION ↗</span>
          </footer>
        </main>
      </div>
    )
  }

  // ==========================================
  // LOGIN / SIGNUP
  // ==========================================

  return (
    <main className="min-h-screen bg-[#0B1220] md:p-5 flex items-center justify-center">

      <div className="w-full max-w-[1450px] min-h-screen md:min-h-[850px] md:rounded-[30px] overflow-hidden grid lg:grid-cols-[1.05fr_.95fr] bg-[#0B1220] border border-white/5 animate-appear">

        {/* LEFT BRAND PANEL */}

        <section className="relative bg-[#121D2E] text-[#F3F4EE] p-7 md:p-12 lg:p-16 flex flex-col justify-between overflow-hidden campus-grid min-h-[520px] lg:min-h-full">

          <div className="relative z-10">
            <Logo />
          </div>

          <div className="relative z-10 max-w-xl py-14">
            <div className="inline-flex items-center gap-2 rounded-full border border-[#C6F36B]/30 px-4 py-2 text-xs text-[#C6F36B] mono-font mb-8">
              <span className="w-2 h-2 rounded-full bg-[#C6F36B] animate-pulse" />
              THE NEXT CHAPTER STARTS HERE
            </div>

            <h1 className="display-font text-5xl md:text-6xl xl:text-7xl font-bold leading-[1.04] tracking-tight">
              Make room
              <br />
              for <span className="text-[#C6F36B]">what's</span>
              <br />
              next.
            </h1>

            <p className="text-[#94A3B8] mt-7 text-base md:text-lg leading-relaxed max-w-md">
              A better beginning deserves a better experience.
              Welcome to a smarter way of managing school admissions.
            </p>

            <div className="mt-12 flex items-center gap-4">
              <div className="flex -space-x-3">
                {['#C6F36B', '#7185FF', '#E7B89C'].map(c => (
                  <div
                    key={c}
                    style={{ background: c }}
                    className="w-10 h-10 rounded-full border-4 border-[#121D2E]"
                  />
                ))}
              </div>

              <div>
                <p className="text-sm font-bold">
                  A new academic journey
                </p>

                <p className="text-xs text-[#94A3B8]">
                  Starts with one step.
                </p>
              </div>
            </div>
          </div>

          <div className="relative z-10 flex justify-between text-[10px] mono-font text-[#94A3B8]/50">
            <span>ADMITLY © 2026</span>
            <span>DESIGNED FOR THE NEXT GENERATION ↗</span>
          </div>

          <div className="absolute -right-36 top-1/4 w-[430px] h-[430px] rounded-full border border-[#C6F36B]/15" />

          <div className="absolute -right-20 top-[32%] w-[300px] h-[300px] rounded-full border border-[#C6F36B]/15" />

          <div className="absolute right-12 bottom-36 float-animation text-[#C6F36B]/70">
            <GraduationCap size={76} strokeWidth={1} />
          </div>
        </section>

        {/* RIGHT AUTH PANEL */}

        <section className="p-7 md:p-12 lg:p-16 flex flex-col justify-center text-[#F3F4EE] bg-[#0B1220]">
          <div className="max-w-md w-full mx-auto animate-appear">

            {/* LOGIN / SIGNUP TABS */}

            <div className="flex gap-2 mb-12 bg-[#121D2E] border border-white/10 p-1 rounded-xl">
              <button
                type="button"
                onClick={() => {
                  setPage('login')
                  setError('')
                }}
                className={`flex-1 py-3 rounded-lg text-sm font-bold transition ${
                  page === 'login'
                    ? 'bg-[#1B2A40] text-[#C6F36B]'
                    : 'text-[#94A3B8] hover:text-[#F3F4EE]'
                }`}
              >
                Sign in
              </button>

              <button
                type="button"
                onClick={() => {
                  setPage('signup')
                  setStep(1)
                  setError('')
                }}
                className={`flex-1 py-3 rounded-lg text-sm font-bold transition ${
                  page === 'signup'
                    ? 'bg-[#1B2A40] text-[#C6F36B]'
                    : 'text-[#94A3B8] hover:text-[#F3F4EE]'
                }`}
              >
                Create account
              </button>
            </div>

            <p className="mono-font text-xs text-[#C6F36B]/70 mb-4 tracking-widest">
              {page === 'login'
                ? 'WELCOME BACK / 01'
                : `YOUR JOURNEY / 0${step}`}
            </p>

            <h2 className="display-font text-4xl md:text-5xl font-bold tracking-tight">
              {page === 'login'
                ? 'Good to see you.'
                : 'Begin your story.'}
            </h2>

            <p className="text-[#94A3B8] mt-3 mb-9">
              {page === 'login'
                ? 'Sign in to continue your journey.'
                : 'Create your student account in a few simple steps.'}
            </p>

            <form
              onSubmit={page === 'login' ? login : register}
              className="space-y-5"
            >

              {/* STUDENT ID */}

              {(page === 'login' || step === 1) && (
                <div>
                  <label className="text-sm font-bold block mb-2">
                    Student ID
                  </label>

                  <input
                    className="auth-input"
                    placeholder="Enter your student ID"
                    value={studentId}
                    onChange={e => setStudentId(e.target.value)}
                    required
                    maxLength={17}
                  />
                </div>
              )}

              {page === 'signup' && step === 2 && (
                <div className="rounded-xl bg-[#1B2A40] border border-white/10 p-4 text-sm text-[#94A3B8]">
                  Student ID:{' '}
                  <strong className="text-[#C6F36B]">
                    {studentId}
                  </strong>
                </div>
              )}

              {/* PASSWORD */}

              {(page === 'login' || step === 2) && (
                <div>
                  <label className="text-sm font-bold block mb-2">
                    Password
                  </label>

                  <div className="relative">
                    <input
                      className="auth-input pr-12"
                      type={showPassword ? 'text' : 'password'}
                      placeholder="Enter your password"
                      value={password}
                      onChange={e => setPassword(e.target.value)}
                      required
                      minLength={8}
                      maxLength={128}
                    />

                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-4 top-1/2 -translate-y-1/2 text-[#94A3B8] hover:text-[#C6F36B]"
                    >
                      {showPassword
                        ? <EyeOff size={19} />
                        : <Eye size={19} />}
                    </button>
                  </div>
                </div>
              )}

              {page === 'login' && (
                <div className="flex justify-between text-xs text-[#94A3B8]">
                  <span>Secure student access</span>
                  <span>JWT protected</span>
                </div>
              )}

              {/* ERROR MESSAGE */}

              {error && (
                <p
                  className={`text-sm rounded-xl p-3 border ${
                    error.startsWith('Account created')
                      ? 'text-[#C6F36B] bg-[#C6F36B]/10 border-[#C6F36B]/20'
                      : 'text-red-300 bg-red-950/40 border-red-500/20'
                  }`}
                >
                  {error}
                </p>
              )}

              {/* SUBMIT BUTTON */}

              {page === 'login' ? (
                <button
                  disabled={loading}
                  className="lime-button w-full flex justify-between items-center disabled:opacity-50"
                >
                  {loading ? 'Signing in...' : 'Enter your space'}
                  <ArrowRight size={19} />
                </button>
              ) : step === 1 ? (
                <button
                  type="button"
                  onClick={() => {
                    if (studentId.trim()) {
                      setStep(2)
                      setError('')
                    } else {
                      setError('Please enter your student ID.')
                    }
                  }}
                  className="lime-button w-full flex justify-between items-center"
                >
                  Continue
                  <ArrowRight size={19} />
                </button>
              ) : (
                <button
                  disabled={loading}
                  className="lime-button w-full flex justify-between items-center disabled:opacity-50"
                >
                  {loading ? 'Creating account...' : 'Create account'}
                  <ArrowRight size={19} />
                </button>
              )}
            </form>

            {page === 'signup' && step === 2 && (
              <button
                type="button"
                onClick={() => setStep(1)}
                className="mt-4 text-sm text-[#94A3B8] hover:text-[#C6F36B]"
              >
                ← Back
              </button>
            )}

            <div className="mt-10 pt-7 border-t border-white/10 flex items-center gap-3 text-xs text-[#94A3B8]">
              <ShieldCheck
                size={18}
                className="text-[#C6F36B] shrink-0"
              />

              Your information is protected with secure authentication.
            </div>
          </div>
        </section>
      </div>
    </main>
  )
}

// ==========================================
// NAVIGATION ITEM
// ==========================================

function NavItem({
  icon,
  label,
  active,
  onClick
}: {
  icon: React.ReactNode
  label: string
  active: boolean
  onClick: () => void
}) {
  return (
    <button
      onClick={onClick}
      className={`w-full p-4 rounded-xl flex items-center gap-3 font-semibold transition ${
        active
          ? 'bg-[#C6F36B] text-[#0B1220] shadow-lg shadow-[#C6F36B]/10'
          : 'text-[#94A3B8] hover:bg-white/5 hover:text-[#F3F4EE]'
      }`}
    >
      {icon}
      {label}
    </button>
  )
}

// ==========================================
// LOGO
// ==========================================

function Logo() {
  return (
    <div className="flex items-center gap-3">
      <div className="w-10 h-10 bg-[#C6F36B] text-[#0B1220] rounded-xl flex items-center justify-center">
        <GraduationCap size={24} />
      </div>

      <div>
        <p className="display-font font-bold text-2xl tracking-tight">
          ADMITLY<span className="text-[#C6F36B]">.</span>
        </p>

        <p className="mono-font text-[8px] tracking-[.22em] text-[#94A3B8]">
          ADMISSIONS REIMAGINED
        </p>
      </div>
    </div>
  )
}

export default App
