CONFIG = -c Release
RUNNER = dotnet run $(CONFIG) --


.PHONY: all init clustering quality tests similarity clean

all: init clustering quality similarity tests


init:
	mkdir -p logs plots
	chmod +x python/*/*.py
	dotnet build
	./python/plotting/data_stats.py


MU_LIST = $(shell seq 1 0.5 5)
THETA_LIST = $(shell seq 1 0.25 3)
CLUSTERING_RUNNER = $(RUNNER) clustering -e $(theta) -u $(mu)
clustering:
	@$(foreach mu, $(MU_LIST), $(foreach theta, $(THETA_LIST), $(CLUSTERING_RUNNER);))
	@$(foreach mu, $(MU_LIST), $(RUNNER) clustering -e 10 -u $(mu);)
	./python/plotting/clustering.py


QMU_LIST = $(shell seq 1 1 5)
QTHETA_LIST = $(shell seq 1 0.25 2)
quick-clustering:
	@$(foreach mu, $(QMU_LIST), $(foreach theta, $(QTHETA_LIST), $(CLUSTERING_RUNNER);))
	@$(foreach mu, $(QMU_LIST), $(RUNNER) clustering -e 10 -u $(mu);)
	./python/plotting/clustering.py


quality:
	$(RUNNER) quality 0.2
	./python/plotting/quality.py logs/Quality.FlashProfile.4.00x1.25.0.20.log


similarity:
	$(RUNNER) similarity -s 16 -d 20
	./python/similarity_baselines/train.py 6,12 22,12 44,12
	./python/similarity_baselines/test.py 6,12 22,12 44,12
	./python/plotting/similarity.py JaroWinkler RF.6.12 RF.22.12 RF.44.12


perf-tests:
	$(RUNNER) tests
	./python/plotting/performance.py


clean:
	rm -rf logs plots
