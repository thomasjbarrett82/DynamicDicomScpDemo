using DynamicDicomScpDemo.Models;

namespace DynamicDicomScpDemo {
    public class FinderServiceFactory {
        private readonly IEnumerable<IDicomImageFinderService> _finders;

        public FinderServiceFactory(IEnumerable<IDicomImageFinderService> finders) {
            _finders = finders;
        }

        public IDicomImageFinderService Create(string finder) {
            var service = _finders.SingleOrDefault(f => f.IsValidFinder(finder));
            if (service == null) {
                throw new InvalidOperationException($"{finder} is not a valid service.");
            }
            return service;
        }
    }
}
