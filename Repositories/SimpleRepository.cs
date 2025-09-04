using SimpleDataEngine.Core;
using SimpleDataEngine.Storage;
using System.Linq.Expressions;

namespace SimpleDataEngine.Repositories
{
    /// <summary>
    /// Simple repository implementation with full feature support
    /// </summary>
    /// <typeparam name="T">Entity type that implements IEntity</typeparam>
    public class SimpleRepository<T> : IEventRepository<T>, IDisposable where T : class, IEntity
    {
        private readonly List<T> _items;
        private readonly IStorage<T> _storage;
        private readonly object _lock = new object();
        private bool _hasUnsavedChanges = false;
        private bool _disposed = false;

        /// <inheritdoc />
        public event Action<T> EntityAdded;

        /// <inheritdoc />
        public event Action<T> EntityUpdated;

        /// <inheritdoc />
        public event Action<T> EntityDeleted;

        /// <inheritdoc />
        public event Action BeforeSave;

        /// <inheritdoc />
        public event Action AfterSave;

        /// <summary>
        /// Initializes a new instance of SimpleRepository
        /// </summary>
        /// <param name="storage">Storage implementation</param>
        public SimpleRepository(IStorage<T> storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _items = _storage.Load() ?? new List<T>();
        }

        /// <inheritdoc />
        public List<T> GetAll()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return new List<T>(_items);
            }
        }

        /// <inheritdoc />
        public T GetById(int id)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _items.FirstOrDefault(x => x.Id == id);
            }
        }

        /// <inheritdoc />
        public void Add(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            lock (_lock)
            {
                ThrowIfDisposed();

                string entityName = typeof(T).Name;
                entity.Id = EntityIndexStorage.GetNextId(entityName);
                entity.UpdateTime = DateTime.Now;

                _items.Add(entity);
                _hasUnsavedChanges = true;

                EntityAdded?.Invoke(entity);
                Save();
            }
        }

        /// <inheritdoc />
        public bool Update(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            lock (_lock)
            {
                ThrowIfDisposed();

                var index = _items.FindIndex(i => i.Id == entity.Id);
                if (index >= 0)
                {
                    entity.UpdateTime = DateTime.Now;
                    _items[index] = entity;
                    _hasUnsavedChanges = true;

                    EntityUpdated?.Invoke(entity);
                    Save();
                    return true;
                }
                return false;
            }
        }

        /// <inheritdoc />
        public bool Delete(int id)
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                var entity = _items.FirstOrDefault(i => i.Id == id);
                if (entity != null)
                {
                    _items.Remove(entity);
                    _hasUnsavedChanges = true;

                    EntityDeleted?.Invoke(entity);
                    Save();
                    return true;
                }
                return false;
            }
        }

        /// <inheritdoc />
        public void Save()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                if (_hasUnsavedChanges)
                {
                    BeforeSave?.Invoke();
                    _storage.Save(_items);
                    _hasUnsavedChanges = false;
                    AfterSave?.Invoke();
                }
            }
        }

        /// <inheritdoc />
        public List<T> Find(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            lock (_lock)
            {
                ThrowIfDisposed();
                return _items.Where(predicate.Compile()).ToList();
            }
        }

        /// <inheritdoc />
        public T FindFirst(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            lock (_lock)
            {
                ThrowIfDisposed();
                return _items.FirstOrDefault(predicate.Compile());
            }
        }

        /// <inheritdoc />
        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return predicate == null ? _items.Count : _items.Count(predicate.Compile());
            }
        }

        /// <inheritdoc />
        public bool Exists(Expression<Func<T, bool>> predicate = null)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return predicate == null ? _items.Any() : _items.Any(predicate.Compile());
            }
        }

        /// <inheritdoc />
        public async Task<List<T>> GetAllAsync()
        {
            return await Task.FromResult(GetAll());
        }

        /// <inheritdoc />
        public async Task<T> GetByIdAsync(int id)
        {
            return await Task.FromResult(GetById(id));
        }

        /// <inheritdoc />
        public async Task AddAsync(T entity)
        {
            await Task.Run(() => Add(entity));
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(T entity)
        {
            return await Task.FromResult(Update(entity));
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id)
        {
            return await Task.FromResult(Delete(id));
        }

        /// <inheritdoc />
        public async Task SaveAsync()
        {
            await Task.Run(() => Save());
        }

        /// <inheritdoc />
        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await Task.FromResult(Find(predicate));
        }

        /// <inheritdoc />
        public async Task<T> FindFirstAsync(Expression<Func<T, bool>> predicate)
        {
            return await Task.FromResult(FindFirst(predicate));
        }

        /// <inheritdoc />
        public void AddRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            lock (_lock)
            {
                ThrowIfDisposed();

                string entityName = typeof(T).Name;
                foreach (var entity in entities)
                {
                    entity.Id = EntityIndexStorage.GetNextId(entityName);
                    entity.UpdateTime = DateTime.Now;
                    _items.Add(entity);
                    EntityAdded?.Invoke(entity);
                }

                _hasUnsavedChanges = true;
                Save();
            }
        }

        /// <inheritdoc />
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await Task.Run(() => AddRange(entities));
        }

        /// <inheritdoc />
        public int UpdateRange(IEnumerable<T> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            lock (_lock)
            {
                ThrowIfDisposed();

                int updatedCount = 0;
                foreach (var entity in entities)
                {
                    var index = _items.FindIndex(i => i.Id == entity.Id);
                    if (index >= 0)
                    {
                        entity.UpdateTime = DateTime.Now;
                        _items[index] = entity;
                        EntityUpdated?.Invoke(entity);
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    _hasUnsavedChanges = true;
                    Save();
                }

                return updatedCount;
            }
        }

        /// <inheritdoc />
        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities)
        {
            return await Task.FromResult(UpdateRange(entities));
        }

        /// <inheritdoc />
        public int DeleteWhere(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            lock (_lock)
            {
                ThrowIfDisposed();

                var itemsToDelete = _items.Where(predicate.Compile()).ToList();
                foreach (var item in itemsToDelete)
                {
                    _items.Remove(item);
                    EntityDeleted?.Invoke(item);
                }

                if (itemsToDelete.Count > 0)
                {
                    _hasUnsavedChanges = true;
                    Save();
                }

                return itemsToDelete.Count;
            }
        }

        /// <inheritdoc />
        public async Task<int> DeleteWhereAsync(Expression<Func<T, bool>> predicate)
        {
            return await Task.FromResult(DeleteWhere(predicate));
        }

        /// <inheritdoc />
        public int DeleteRange(IEnumerable<int> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            lock (_lock)
            {
                ThrowIfDisposed();

                int deletedCount = 0;
                foreach (var id in ids)
                {
                    var item = _items.FirstOrDefault(i => i.Id == id);
                    if (item != null)
                    {
                        _items.Remove(item);
                        EntityDeleted?.Invoke(item);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    _hasUnsavedChanges = true;
                    Save();
                }

                return deletedCount;
            }
        }

        /// <inheritdoc />
        public async Task<int> DeleteRangeAsync(IEnumerable<int> ids)
        {
            return await Task.FromResult(DeleteRange(ids));
        }

        /// <summary>
        /// Gets the total number of entities
        /// </summary>
        public int TotalCount => Count();

        /// <summary>
        /// Indicates if there are unsaved changes
        /// </summary>
        public bool HasUnsavedChanges
        {
            get
            {
                lock (_lock)
                {
                    return _hasUnsavedChanges;
                }
            }
        }

        /// <summary>
        /// Forces a reload of data from storage
        /// </summary>
        public void Reload()
        {
            lock (_lock)
            {
                ThrowIfDisposed();

                _items.Clear();
                var reloadedItems = _storage.Load();
                if (reloadedItems != null)
                {
                    _items.AddRange(reloadedItems);
                }
                _hasUnsavedChanges = false;
            }
        }

        /// <summary>
        /// Asynchronously reloads data from storage
        /// </summary>
        public async Task ReloadAsync()
        {
            await Task.Run(() => Reload());
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SimpleRepository<T>));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                Save(); // Save any pending changes
                _disposed = true;
            }
        }
    }
}