import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks';
import { ErrorMessage } from '../components/ErrorMessage';
import { LabelChip } from '../components/LabelChip';
import { dueStatus, formatDue, todayIso } from '../lib/dates';
import { DUE_OPTIONS } from '../lib/filters';
import { queryKeys } from '../lib/queryKeys';
import { usePageTitle } from '../lib/usePageTitle';
import { groupTasks } from '../lib/tasks';

export function MyTasksPage() {
  const [params, setParams] = useSearchParams();
  const due = params.get('due') ?? '';
  const includeCompleted = params.get('completed') === 'true';
  const tasks = useQuery({
    queryKey: queryKeys.myTasks({ due, completed: String(includeCompleted) }),
    queryFn: () => tasksApi.mine({ due, includeCompleted }),
  });

  function update(key: string, value: string) {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    setParams(next, { replace: true });
  }

  const today = todayIso();
  usePageTitle('My tasks');

  return (
    <section className="my-tasks">
      <header className="page-header">
        <div>
          <h1>My tasks</h1>
          <p className="muted">Cards assigned to you on every board.</p>
        </div>
        <div className="row">
          <select aria-label="Due" value={due} onChange={(e) => update('due', e.target.value)}>
            <option value="">Any due date</option>
            {DUE_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </select>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={includeCompleted}
              onChange={(e) => update('completed', e.target.checked ? 'true' : '')}
            />
            Show completed
          </label>
        </div>
      </header>

      <ErrorMessage error={tasks.error} />
      {tasks.isPending && <p className="muted">Loading…</p>}
      {tasks.data?.length === 0 && (
        <div className="empty-state">
          <p>Nothing assigned to you{due ? ' with that due date' : ''}. Enjoy the quiet.</p>
        </div>
      )}
      {tasks.data &&
        groupTasks(tasks.data, today).map((group) => (
          <section key={group.key} className="task-group">
            <h2>
              {group.title} <span className="muted">{group.tasks.length}</span>
            </h2>
            <ul className="task-list">
              {group.tasks.map((task) => (
                <li key={task.id} className={task.completed ? 'task completed' : 'task'}>
                  <Link to={`/b/${task.boardId}?card=${task.id}`} className="task-title">
                    {task.title}
                  </Link>
                  <span className="label-row">
                    {task.labels.map((label) => (
                      <LabelChip key={label.id} label={label} />
                    ))}
                  </span>
                  <span className="muted small task-where">
                    {task.workspaceName} › {task.boardName} › {task.columnName}
                  </span>
                  {task.dueDate && (
                    <span
                      className={`due-badge due-${dueStatus(task.dueDate, task.completed, today)}`}
                    >
                      {formatDue(task.dueDate, today)}
                    </span>
                  )}
                </li>
              ))}
            </ul>
          </section>
        ))}
    </section>
  );
}
