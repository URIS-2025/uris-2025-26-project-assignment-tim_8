import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Plus, Settings, Tags, Trash2 } from 'lucide-react';
import DataTable from '../components/DataTable';
import StatusBadge from '../components/StatusBadge';
import Modal from '../components/Modal';
import './BoxSettings.css';

// Mock Data matching the Box Category aggregations
const mockCategories = [
    { id: 'CAT-001', name: 'Workplace Safety', description: 'Reports of physical hazards', type: 'Problem', status: 'Active' },
    { id: 'CAT-002', name: 'Amenities', description: 'Requests for kitchen or breakroom improvements', type: 'Suggestion', status: 'Active' },
    { id: 'CAT-003', name: 'IT Systems', description: 'Network or hardware issues', type: 'Problem', status: 'Active' },
    { id: 'CAT-004', name: 'HR/Personnel', description: 'Sensitive employee relations feedback', type: 'Problem', status: 'Inactive' }
];

const BoxSettings = () => {
    const { boxId } = useParams();
    const navigate = useNavigate();
    const [isCategoryModalOpen, setCategoryModalOpen] = useState(false);

    const columns = [
        { header: 'Category Name', accessor: 'name', render: (row) => <strong>{row.name}</strong> },
        { header: 'Description', accessor: 'description' },
        { header: 'Type', accessor: 'type' },
        {
            header: 'Status',
            accessor: 'status',
            render: (row) => <StatusBadge type="status" status={row.status === 'Active' ? 'Open' : 'Closed'} />
        },
        {
            header: 'Actions',
            accessor: 'actions',
            render: (row) => (
                <button className="btn btn-ghost icon-btn small text-danger" title="Delete Category" aria-label="Delete Category">
                    <Trash2 size={16} />
                </button>
            )
        }
    ];

    return (
        <div className="box-settings-container animate-fade-in">

            <button type="button" className="back-link" onClick={() => navigate(`/admin/boxes/${boxId || 'BOX-201'}`)}>
                <ArrowLeft size={16} /> Back to Box Details
            </button>

            <div className="page-header">
                <div>
                    <h1 className="page-title">Box Settings</h1>
                    <p className="page-description">Configure categories, tags, and automation rules for this box.</p>
                </div>
            </div>

            <div className="glass-panel box-settings-panel">
                <div className="box-settings-section-head">
                    <Settings size={24} className="text-secondary" />
                    <h2 className="box-settings-section-title">General Settings</h2>
                </div>

                <div className="form-group box-settings-field">
                    <label>Box Name</label>
                    <input type="text" className="form-control" defaultValue="Facilities & Maintenance" />
                </div>
                <div className="form-group box-settings-field">
                    <label>Visibility Status</label>
                    <select className="form-control" defaultValue="Active">
                        <option value="Active">Active (Accepting Submissions)</option>
                        <option value="Paused">Paused (Temporary hold)</option>
                        <option value="Closed">Closed (Archived)</option>
                    </select>
                </div>
                <button className="btn btn-primary box-settings-save">Save Changes</button>
            </div>

            <div className="table-wrapper">
                <div className="box-settings-table-head">
                    <div className="box-settings-table-head-left">
                        <Tags size={24} className="text-secondary" />
                        <h2 className="box-settings-section-title">Categories</h2>
                    </div>
                    <button className="btn btn-primary" onClick={() => setCategoryModalOpen(true)}>
                        <Plus size={16} className="mr-2" /> Add Category
                    </button>
                </div>
                <DataTable
                    data={mockCategories}
                    columns={columns}
                    searchPlaceholder="Search categories..."
                />
            </div>

            <Modal
                isOpen={isCategoryModalOpen}
                onClose={() => setCategoryModalOpen(false)}
                title="Create New Category"
                footer={
                    <>
                        <button className="btn btn-ghost" onClick={() => setCategoryModalOpen(false)}>Cancel</button>
                        <button className="btn btn-primary" onClick={() => setCategoryModalOpen(false)}>Save Category</button>
                    </>
                }
            >
                <div className="form-group">
                    <label>Category Name</label>
                    <input type="text" className="form-control" placeholder="e.g., Harassment" />
                </div>
                <div className="form-group">
                    <label>Applies To</label>
                    <select className="form-control">
                        <option value="Both">Both Problems and Suggestions</option>
                        <option value="Problem">Problems Only</option>
                        <option value="Suggestion">Suggestions Only</option>
                    </select>
                </div>
                <div className="form-group">
                    <label>Description</label>
                    <textarea className="form-control" rows="3" placeholder="Brief explanation of what belongs in this category..."></textarea>
                </div>
            </Modal>
        </div>
    );
};

export default BoxSettings;
