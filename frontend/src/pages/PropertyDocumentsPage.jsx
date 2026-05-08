import CrudPage from '../components/CrudPage'

const fields = [
  { name: 'propertyId', label: 'Property Id', type: 'int', required: true },
  {
    name: 'documentType',
    label: 'Document Type',
    type: 'select',
    options: ['Title', 'TaxDeclaration', 'DeedOfSale', 'SurveyPlan', 'Other'],
    required: true,
  },
  { name: 'documentName', label: 'Document Name', type: 'text', required: true },
  { name: 'filePath', label: 'File Path', type: 'text', required: true },
  { name: 'fileSize', label: 'File Size', type: 'int' },
  { name: 'uploadedBy', label: 'Uploaded By', type: 'int' },
  { name: 'uploadedAt', label: 'Uploaded At', type: 'datetime-local', inForm: false },
]

export default function PropertyDocumentsPage() {
  return (
    <CrudPage
      title="Property Documents"
      endpoint="/PropertyDocuments"
      idField="documentId"
      fields={fields}
    />
  )
}
