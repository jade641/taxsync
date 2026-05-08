import { useEffect, useMemo, useState } from 'react'
import api from '../api'

const buildEmptyForm = (fields, idField) => {
  const empty = { [idField]: '' }
  fields.forEach((field) => {
    empty[field.name] = ''
  })
  return empty
}

const formatForInput = (value, type) => {
  if (value === null || value === undefined) {
    return ''
  }

  if (type === 'date') {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return ''
    return date.toISOString().slice(0, 10)
  }

  if (type === 'datetime-local') {
    const date = new Date(value)
    if (Number.isNaN(date.getTime())) return ''
    return date.toISOString().slice(0, 16)
  }

  if (type === 'boolean') {
    return String(value)
  }

  return String(value)
}

const formatForDisplay = (value, type) => {
  if (value === null || value === undefined || value === '') {
    return ''
  }

  if (type === 'date') {
    const date = new Date(value)
    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleDateString()
  }

  if (type === 'datetime-local') {
    const date = new Date(value)
    return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString()
  }

  if (type === 'boolean') {
    return value ? 'true' : 'false'
  }

  return String(value)
}

const coerceValue = (value, type) => {
  if (value === '') {
    return null
  }

  if (type === 'int') {
    return value === null ? null : Number.parseInt(value, 10)
  }

  if (type === 'number') {
    return value === null ? null : Number.parseFloat(value)
  }

  if (type === 'boolean') {
    if (value === true || value === false) return value
    return value === 'true'
  }

  return value
}

export default function CrudPage({ title, endpoint, idField, fields }) {
  const [items, setItems] = useState([])
  const [form, setForm] = useState(() => buildEmptyForm(fields, idField))
  const [editingId, setEditingId] = useState(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const formFields = useMemo(
    () => fields.filter((field) => field.inForm !== false),
    [fields],
  )
  const tableFields = useMemo(
    () => fields.filter((field) => field.inTable !== false),
    [fields],
  )

  const loadItems = async () => {
    setLoading(true)
    setError('')
    try {
      const response = await api.get(endpoint)
      setItems(Array.isArray(response.data) ? response.data : [])
    } catch (err) {
      setError(err?.response?.data?.message || err.message || 'Failed to load data')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadItems()
  }, [endpoint])

  const handleChange = (event, field) => {
    const { value } = event.target
    setForm((prev) => ({ ...prev, [field.name]: value }))
  }

  const resetForm = () => {
    setForm(buildEmptyForm(fields, idField))
    setEditingId(null)
  }

  const handleEdit = (item) => {
    const nextForm = { [idField]: item[idField] }
    fields.forEach((field) => {
      nextForm[field.name] = formatForInput(item[field.name], field.type)
    })
    setForm(nextForm)
    setEditingId(item[idField])
  }

  const handleDelete = async (itemId) => {
    if (!window.confirm('Delete this record?')) return
    setError('')
    try {
      await api.delete(`${endpoint}/${itemId}`)
      await loadItems()
    } catch (err) {
      setError(err?.response?.data?.message || err.message || 'Failed to delete record')
    }
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')

    const payload = {}
    fields.forEach((field) => {
      payload[field.name] = coerceValue(form[field.name], field.type)
    })

    if (editingId !== null) {
      payload[idField] = editingId
    }

    try {
      if (editingId === null) {
        await api.post(endpoint, payload)
      } else {
        await api.put(`${endpoint}/${editingId}`, payload)
      }

      resetForm()
      await loadItems()
    } catch (err) {
      setError(err?.response?.data?.message || err.message || 'Failed to save record')
    }
  }

  return (
    <div style={{ padding: '24px' }}>
      <h2 style={{ marginBottom: '16px' }}>{title}</h2>

      <form onSubmit={handleSubmit} style={{ display: 'grid', gap: '12px', marginBottom: '24px' }}>
        {formFields.map((field) => (
          <label key={field.name} style={{ display: 'grid', gap: '6px' }}>
            <span>{field.label}</span>
            {field.type === 'select' ? (
              <select
                value={form[field.name]}
                onChange={(event) => handleChange(event, field)}
                required={field.required}
              >
                <option value="">Select...</option>
                {field.options?.map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>
            ) : field.type === 'textarea' ? (
              <textarea
                value={form[field.name]}
                onChange={(event) => handleChange(event, field)}
                required={field.required}
                rows={3}
              />
            ) : field.type === 'boolean' ? (
              <select
                value={form[field.name]}
                onChange={(event) => handleChange(event, field)}
                required={field.required}
              >
                <option value="">Select...</option>
                <option value="true">true</option>
                <option value="false">false</option>
              </select>
            ) : (
              <input
                type={field.type || 'text'}
                value={form[field.name]}
                onChange={(event) => handleChange(event, field)}
                required={field.required}
              />
            )}
          </label>
        ))}

        <div style={{ display: 'flex', gap: '8px' }}>
          <button type="submit">{editingId === null ? 'Create' : 'Update'}</button>
          <button type="button" onClick={resetForm}>
            Clear
          </button>
        </div>
      </form>

      {error && <div style={{ color: 'crimson', marginBottom: '12px' }}>{error}</div>}
      {loading && <div>Loading...</div>}

      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ borderBottom: '1px solid #ddd', padding: '8px' }}>{idField}</th>
              {tableFields.map((field) => (
                <th key={field.name} style={{ borderBottom: '1px solid #ddd', padding: '8px' }}>
                  {field.label}
                </th>
              ))}
              <th style={{ borderBottom: '1px solid #ddd', padding: '8px' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item[idField]}>
                <td style={{ borderBottom: '1px solid #eee', padding: '8px' }}>{item[idField]}</td>
                {tableFields.map((field) => (
                  <td key={field.name} style={{ borderBottom: '1px solid #eee', padding: '8px' }}>
                    {formatForDisplay(item[field.name], field.type)}
                  </td>
                ))}
                <td style={{ borderBottom: '1px solid #eee', padding: '8px' }}>
                  <button type="button" onClick={() => handleEdit(item)}>
                    Edit
                  </button>
                  <button type="button" onClick={() => handleDelete(item[idField])}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr>
                <td colSpan={tableFields.length + 2} style={{ padding: '12px', textAlign: 'center' }}>
                  No records found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
